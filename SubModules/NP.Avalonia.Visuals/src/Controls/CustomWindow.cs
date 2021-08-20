﻿// (c) Nick Polyak 2021 - http://awebpros.com/
// License: MIT License (https://opensource.org/licenses/MIT)
//
// short overview of copyright rules:
// 1. you can use this framework in any commercial or non-commercial 
//    product as long as you retain this copyright message
// 2. Do not blame the author of this software if something goes wrong. 
// 
// Also, please, mention this software in any documentation for the 
// products that use it.
//
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using NP.Avalonia.Visuals.Behaviors;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace NP.Avalonia.Visuals.Controls
{
    public class CustomWindow : Window, IStyleable
    {
        private (string ControlName, StandardCursorType CursorType, WindowEdge TheWindowEdge)[] ResizeCursorInfos =
        {
            ("Left", StandardCursorType.LeftSide, WindowEdge.West),
            ("Right", StandardCursorType.RightSide, WindowEdge.East),
            ("Top", StandardCursorType.TopSide, WindowEdge.North),
            ("Bottom", StandardCursorType.BottomSide, WindowEdge.South),
            ("TopLeft", StandardCursorType.TopLeftCorner, WindowEdge.NorthWest),
            ("TopRight", StandardCursorType.TopRightCorner, WindowEdge.NorthEast),
            ("BottomLeft", StandardCursorType.BottomLeftCorner, WindowEdge.SouthWest),
            ("BottomRight", StandardCursorType.BottomRightCorner, WindowEdge.SouthEast)
        };

        private Control? _headerControl = null;

        public Control? HeaderControl => _headerControl;

        IDisposable _windowStateChangeDisposer;

        IDisposable _windowCustomFeatureDisposer;

        public CustomWindow()
        {
#if DEBUG
            this.AttachDevTools();
#endif
            (this as INotifyPropertyChanged).PropertyChanged += CustomWindow_PropertyChanged;

            _windowStateChangeDisposer =
                WindowStateProperty.Changed.Subscribe(OnWindowStateChanged);

            _windowCustomFeatureDisposer =
                HasCustomWindowFeaturesProperty.Changed.Subscribe(OnHasCustomFeaturesChanged);
        }


        private void OnHasCustomFeaturesChanged(AvaloniaPropertyChangedEventArgs<bool> hasCustomFeaturesContainer)
        {
            if (!hasCustomFeaturesContainer.NewValue.Value)
            {
                SetIsHitVisibleOnResizeControls(false);
                this.IsCustomHeaderVisible = false;
                this.SystemDecorations = SystemDecorations.Full;
            }
            else
            {
                SetIsHitVisibleOnResizeControls(true);
                this.IsCustomHeaderVisible = true;
            }
        }

        private void OnWindowStateChanged(AvaloniaPropertyChangedEventArgs<WindowState> windowStateChange)
        {
            WindowState windowState = windowStateChange.NewValue.Value;

            if (windowState == WindowState.Maximized)
            {
                SetIsHitVisibleOnResizeControls(false);
            }

            WindowState oldWindowState = windowStateChange.OldValue.Value;

            if (oldWindowState == WindowState.Maximized)
            {
                if (HasCustomWindowFeatures)
                {
                    SetIsHitVisibleOnResizeControls(true);
                }
            }
        }

        private void CustomWindow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WindowState))
            {
                RaisePropertyChanged(CanMaximizeProperty, !CanMaximize, CanMaximize);
                RaisePropertyChanged(CanRestoreProperty, !CanRestore, CanRestore);
            }
        }

        private Control GetControlByName(string name)
        {
            return this.GetVisualDescendants().OfType<Control>().First(b => b.Name == name);
        }

        private Control SetCursorGetControl(string name, StandardCursorType cursorType)
        {
            var ctl = GetControlByName(name);
            ctl.Cursor = new Cursor(cursorType);

            return ctl;
        }

        private void MaximizeOrRestore()
        {
            if (WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        public static readonly DirectProperty<CustomWindow, bool> CanMaximizeProperty =
            AvaloniaProperty.RegisterDirect<CustomWindow, bool>
            (
                nameof(CanMaximize),
                o => o.CanMaximize);

        public bool CanMaximize => this.WindowState != WindowState.Maximized;

        public void Maximize()
        {
            this.WindowState = WindowState.Maximized;
        }

        public static readonly DirectProperty<CustomWindow, bool> CanRestoreProperty =
            AvaloniaProperty.RegisterDirect<CustomWindow, bool>
            (
                nameof(CanRestore),
                o => o.CanRestore);

        public bool CanRestore =>
            this.WindowState == WindowState.Maximized || this.WindowState == WindowState.FullScreen;

        public void Restore()
        {
            this.WindowState = WindowState.Normal;
        }

        public void Minimize()
        {
            this.WindowState = WindowState.Minimized;
        }

        void SetupSide(string name, StandardCursorType cursorType, WindowEdge edge)
        {
            var ctl = SetCursorGetControl(name, cursorType);

            EventHandler<PointerPressedEventArgs>? handler = null;

            handler = (i, e) =>
            {
                Cursor? oldWindowCursor = this.Cursor;
                this.Cursor = new Cursor(cursorType);

                SetIsHitVisibleOnResizeControls(false);
                PlatformImpl?.BeginResizeDrag(edge, e);
                SetIsHitVisibleOnResizeControls(true);

                this.Cursor = oldWindowCursor;
            };

            ctl.PointerPressed += handler;
        }

        private void SetIsHitVisibleOnResizeControls(bool isHitVisible)
        {
            foreach (var resizeCursorInfo in ResizeCursorInfos)
            {
                var ctl = GetControlByName(resizeCursorInfo.ControlName);
                ctl.IsHitTestVisible = isHitVisible;
            }
        }

        Type IStyleable.StyleKey => typeof(CustomWindow);

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            foreach (var resizeCursorInfo in ResizeCursorInfos)
            {
                SetupSide(resizeCursorInfo.ControlName, resizeCursorInfo.CursorType, resizeCursorInfo.TheWindowEdge);
            }

            _headerControl = this.GetControlByName("PART_HeaderControl");

            _headerControl.PointerPressed += OnHeaderPointerPressed;

            _headerControl.DoubleTapped += _headerControl_DoubleTapped;
        }

        private void _headerControl_DoubleTapped(object? sender, RoutedEventArgs e)
        {
            MaximizeOrRestore();
        }

        protected virtual void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            BeginMoveDrag(e);
        }

        #region HeaderTemplate Avalonia Property
        public ControlTemplate HeaderTemplate
        {
            get { return GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        public static readonly StyledProperty<ControlTemplate> HeaderTemplateProperty =
            AvaloniaProperty.Register<CustomWindow, ControlTemplate>
            (
                "HeaderTemplate"
            );
        #endregion HeaderTemplate Avalonia Property


        #region ResizeMargin Avalonia Property
        public double ResizeMargin
        {
            get { return GetValue(ResizeMarginProperty); }
            set { SetValue(ResizeMarginProperty, value); }
        }

        public static readonly AttachedProperty<double> ResizeMarginProperty =
            AvaloniaProperty.RegisterAttached<CustomWindow, Control, double>
            (
                "ResizeMargin"
            );
        #endregion ResizeMargin Avalonia Property


        #region HeaderBackground Avalonia Property
        public IBrush HeaderBackground
        {
            get { return GetValue(HeaderBackgroundProperty); }
            set { SetValue(HeaderBackgroundProperty, value); }
        }

        public static readonly AttachedProperty<IBrush> HeaderBackgroundProperty =
            AvaloniaProperty.RegisterAttached<CustomWindow, Control, IBrush>
            (
                "HeaderBackground"
            );
        #endregion HeaderBackground Avalonia Property


        #region HeaderHeight Avalonia Property
        public double HeaderHeight
        {
            get { return GetValue(HeaderHeightProperty); }
            set { SetValue(HeaderHeightProperty, value); }
        }

        public static readonly AttachedProperty<double> HeaderHeightProperty =
            AvaloniaProperty.RegisterAttached<CustomWindow, Control, double>
            (
                "HeaderHeight"
            );
        #endregion HeaderHeight Avalonia Property


        #region HeaderSeparatorHeight Avalonia Property
        public double HeaderSeparatorHeight
        {
            get { return GetValue(HeaderSeparatorHeightProperty); }
            set { SetValue(HeaderSeparatorHeightProperty, value); }
        }

        public static readonly AttachedProperty<double> HeaderSeparatorHeightProperty =
            AvaloniaProperty.RegisterAttached<CustomWindow, Control, double>
            (
                "HeaderSeparatorHeight"
            );
        #endregion HeaderSeparatorHeight Avalonia Property


        #region HeaderSeparatorBrush Avalonia Property
        public IBrush HeaderSeparatorBrush
        {
            get { return GetValue(HeaderSeparatorBrushProperty); }
            set { SetValue(HeaderSeparatorBrushProperty, value); }
        }

        public static readonly AttachedProperty<IBrush> HeaderSeparatorBrushProperty =
            AvaloniaProperty.RegisterAttached<CustomWindow, Control, IBrush>
            (
                "HeaderSeparatorBrush"
            );
        #endregion HeaderSeparatorBrush Avalonia Property


        #region CustomHeaderIcon Avalonia Property
        public IBitmap CustomHeaderIcon
        {
            get { return GetValue(CustomHeaderIconProperty); }
            set { SetValue(CustomHeaderIconProperty, value); }
        }

        public static readonly AttachedProperty<IBitmap> CustomHeaderIconProperty =
            AvaloniaProperty.RegisterAttached<CustomWindow, Control, IBitmap>
            (
                "CustomHeaderIcon"
            );
        #endregion CustomHeaderIcon Avalonia Property


        #region CustomHeaderIconWidth Avalonia Property
        public double CustomHeaderIconWidth
        {
            get { return GetValue(CustomHeaderIconWidthProperty); }
            set { SetValue(CustomHeaderIconWidthProperty, value); }
        }

        public static readonly AttachedProperty<double> CustomHeaderIconWidthProperty =
            AvaloniaProperty.RegisterAttached<CustomWindow, Control, double>
            (
                "CustomHeaderIconWidth",
                double.NaN
            );
        #endregion CustomHeaderIconWidth Avalonia Property


        #region CustomHeaderIconHeight Avalonia Property
        public double CustomHeaderIconHeight
        {
            get { return GetValue(CustomHeaderIconHeightProperty); }
            set { SetValue(CustomHeaderIconHeightProperty, value); }
        }

        public static readonly AttachedProperty<double> CustomHeaderIconHeightProperty =
            AvaloniaProperty.RegisterAttached<CustomWindow, Control, double>
            (
                "CustomHeaderIconHeight",
                double.NaN
            );
        #endregion CustomHeaderIconHeight Avalonia Property


        #region CustomHeaderIconMargin Avalonia Property
        public Thickness CustomHeaderIconMargin
        {
            get { return GetValue(CustomHeaderIconMarginProperty); }
            set { SetValue(CustomHeaderIconMarginProperty, value); }
        }

        public static readonly AttachedProperty<Thickness> CustomHeaderIconMarginProperty =
            AvaloniaProperty.RegisterAttached<CustomWindow, Control, Thickness>
            (
                "CustomHeaderIconMargin"
            );
        #endregion CustomHeaderIconMargin Avalonia Property


        #region TitleClasses Styled Avalonia Property
        public string TitleClasses
        {
            get { return GetValue(TitleClassesProperty); }
            set { SetValue(TitleClassesProperty, value); }
        }

        public static readonly StyledProperty<string> TitleClassesProperty =
            AvaloniaProperty.Register<CustomWindow, string>
            (
                nameof(TitleClasses)
            );
        #endregion TitleClasses Styled Avalonia Property

        #region HeaderContent Styled Avalonia Property
        public object HeaderContent
        {
            get { return GetValue(HeaderContentProperty); }
            set { SetValue(HeaderContentProperty, value); }
        }

        public static readonly StyledProperty<object> HeaderContentProperty =
            AvaloniaProperty.Register<CustomWindow, object>
            (
                nameof(HeaderContent)
            );
        #endregion HeaderContent Styled Avalonia Property

        #region HeaderContentTemplate Styled Avalonia Property
        public IDataTemplate HeaderContentTemplate
        {
            get { return GetValue(HeaderContentTemplateProperty); }
            set { SetValue(HeaderContentTemplateProperty, value); }
        }

        public static readonly StyledProperty<IDataTemplate> HeaderContentTemplateProperty =
            AvaloniaProperty.Register<CustomWindow, IDataTemplate>
            (
                nameof(HeaderContentTemplate)
            );
        #endregion HeaderContentTemplate Styled Avalonia Property


        #region IsCustomHeaderVisible Styled Avalonia Property
        public bool IsCustomHeaderVisible
        {
            get { return GetValue(IsCustomHeaderVisibleProperty); }
            set { SetValue(IsCustomHeaderVisibleProperty, value); }
        }

        public static readonly StyledProperty<bool> IsCustomHeaderVisibleProperty =
            AvaloniaProperty.Register<CustomWindow, bool>
            (
                nameof(IsCustomHeaderVisible),
                true
            );
        #endregion IsCustomHeaderVisible Styled Avalonia Property


        #region HasCustomWindowFeatures Styled Avalonia Property
        public bool HasCustomWindowFeatures
        {
            get { return GetValue(HasCustomWindowFeaturesProperty); }
            set { SetValue(HasCustomWindowFeaturesProperty, value); }
        }

        public static readonly StyledProperty<bool> HasCustomWindowFeaturesProperty =
            AvaloniaProperty.Register<CustomWindow, bool>
            (
                nameof(HasCustomWindowFeatures), 
                true
            );
        #endregion HasCustomWindowFeatures Styled Avalonia Property

    }
}
