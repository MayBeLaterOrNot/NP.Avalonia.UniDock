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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using NP.Concepts.Behaviors;
using NP.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace NP.Avalonia.UniDock
{
    public class TabbedDockGroup : TemplatedControl, ILeafDockObj
    {
        public event Action<IDockGroup>? IsDockVisibleChangedEvent;

        void IDockGroup.FireIsDockVisibleChangedEvent()
        {
            IsDockVisibleChangedEvent?.Invoke(this);
        }

        public event Action<IDockGroup>? DockIdChanged;

        #region DockId Styled Avalonia Property
        public string DockId
        {
            get { return GetValue(DockIdProperty); }
            set { SetValue(DockIdProperty, value); }
        }

        public static readonly StyledProperty<string> DockIdProperty =
            DockIdContainingControl.DockIdProperty.AddOwner<TabbedDockGroup>();
        #endregion Id Styled Avalonia Property

        private bool _isStableGroup = false;
        public bool IsStableGroup 
        { 
            get => _isStableGroup; 
            set
            {
                if (_isStableGroup == value)
                    return;

                _isStableGroup = value;
                SetHorizontalAndVerticalDockingAllowed();
            }
        }


        #region IsActive Styled Avalonia Property
        public bool IsActive
        {
            get { return GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        public static readonly StyledProperty<bool> IsActiveProperty =
            AvaloniaProperty.Register<TabbedDockGroup, bool>
            (
                nameof(IsActive)
            );
        #endregion IsActive Styled Avalonia Property


        #region IsFullyActive Styled Avalonia Property
        public bool IsFullyActive
        {
            get { return GetValue(IsFullyActiveProperty); }
            set { SetValue(IsFullyActiveProperty, value); }
        }

        public static readonly StyledProperty<bool> IsFullyActiveProperty =
            AvaloniaProperty.Register<TabbedDockGroup, bool>
            (
                nameof(IsFullyActive)
            );
        #endregion IsFullyActive Styled Avalonia Property


        private void SetHorizontalAndVerticalDockingAllowed()
        {
            if (_isStableGroup)
            {
                StackDockGroup? parentGroup = DockParent as StackDockGroup;

                AllowHorizontalDocking = parentGroup?.TheOrientation == Orientation.Horizontal;
                AllowVerticalDocking = parentGroup?.TheOrientation == Orientation.Vertical;
            }
            else
            {
                AllowHorizontalDocking = true;
                AllowVerticalDocking = true;
                return;
            }
        }

        private void FireDockIdChanged()
        {
            DockIdChanged?.Invoke(this);
        }

        static TabbedDockGroup()
        {
            DockIdProperty.Changed.AddClassHandler<TabbedDockGroup>((g, e) => g.OnDockIdChanged(e));

            TabStripPlacementProperty.Changed.AddClassHandler<TabbedDockGroup>((sender, e) => sender.OnTabStringPlacementChanged(e));
        }

        private void OnTabStringPlacementChanged(AvaloniaPropertyChangedEventArgs e)
        {
            OnTabStringPlacementChanged();
        }

        private void OnTabStringPlacementChanged()
        {
            this.TabOrientation =
                (TabStripPlacement == Dock.Top || TabStripPlacement == Dock.Bottom) 
                    ? 
                    Orientation.Horizontal : Orientation.Vertical;
        }

        private void OnDockIdChanged(AvaloniaPropertyChangedEventArgs e)
        {
            FireDockIdChanged();
        }

        public DockManager? TheDockManager
        {
            get => DockAttachedProperties.GetTheDockManager(this);
            set => DockAttachedProperties.SetTheDockManager(this, value);
        }

        IDisposable? _setItemsBehavior;

        public event Action<IRemovable>? RemoveEvent;

        public void Remove()
        {
            RemoveEvent?.Invoke(this);
        }

        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly DirectProperty<TabbedDockGroup, IList<IDockGroup>?> ItemsProperty =
            AvaloniaProperty.RegisterDirect<TabbedDockGroup, IList<IDockGroup>?>
            (
                nameof(Items),
                o => o.Items,
                (o, v) => o.Items = v);

        private IList<IDockGroup>? _items = new ObservableCollection<IDockGroup>();
        /// <summary>
        /// Gets or sets the items to display.
        /// </summary>
        [Content]
        public IList<IDockGroup>? Items
        {
            get
            {
                return _items;
            }

            set
            {
                DisposeBehavior();

                SetAndRaise(ItemsProperty!, ref _items, value);

                SetBehavior();
            }
        }

        public IList<IDockGroup>? DockChildren => Items;

        public DropPanelWithCompass? DropPanel =>
            this.GetVisualDescendants().OfType<DropPanelWithCompass>().FirstOrDefault();

        public DockKind? CurrentGroupDock =>
            DropPanel?.DockSide;

        private IDockGroup? _dockParent;
        public IDockGroup? DockParent
        {
            get => _dockParent;
            set
            {
                if (_dockParent == value)
                    return;

                _dockParent = value;

                SetHorizontalAndVerticalDockingAllowed();
            }
        }

        private readonly SingleSelectionBehavior<DockItem> _singleSelectionBehavior =
            new SingleSelectionBehavior<DockItem>();

        private readonly MimicCollectionBehavior<IDockGroup, DockItem, ObservableCollection<DockItem>> _mimicCollectionBehavior =
            new MimicCollectionBehavior<IDockGroup, DockItem, ObservableCollection<DockItem>>(dockGroup => (DockItem)dockGroup);

        public TabbedDockGroup()
        {
            AffectsMeasure<DockTabsPresenter>(TabStripPlacementProperty);
            AffectsMeasure<TabbedDockGroup>(DockAttachedProperties.IsDockVisibleProperty);

            SetBehavior();

            _singleSelectionBehavior.PropertyChanged += 
                _singleSelectionBehavior_PropertyChanged;

            SetSelectedItem();

            OnTabStringPlacementChanged();

            this.AddHandler(InputElement.PointerPressedEvent, OnTabbedDockGroupPressed, RoutingStrategies.Bubble, true);
        }

        private void OnTabbedDockGroupPressed(object? sender, PointerPressedEventArgs e)
        {
            if (SelectedItem is IActiveItem<DockItem> dockItem)
            {
                dockItem.MakeActive();
            }
        }

        private void _singleSelectionBehavior_PropertyChanged
        (
            object? sender, 
            PropertyChangedEventArgs e)
        {
            SetSelectedItem();
        }

        private IDisposable? _isActiveBinding = null;
        private void SetSelectedItem()
        {
            _isActiveBinding?.Dispose();

            SelectedItem = _singleSelectionBehavior.TheSelectedItem;

            if (SelectedItem is DockItem dockItem)
            {
                Binding binding = new Binding();
                binding.Source = dockItem;
                binding.Path = "IsActive";
                binding.Mode = BindingMode.OneWay;
                _isActiveBinding = this.Bind(IsActiveProperty, binding);
            }
        }

        public void ClearSelectedItem()
        {
            _singleSelectionBehavior.TheSelectedItem = null!;
        }

        public void ClearSelfOnRemove()
        {
            ClearSelectedItem();
            this.TheDockManager = null;
        }

        public void SelectFirst()
        {
            SelectFirstVisibleChild();
        }


        #region SelectedItem Styled Avalonia Property
        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            private set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly StyledProperty<object> SelectedItemProperty =
            AvaloniaProperty.Register<TabbedDockGroup, object>
            (
                nameof(SelectedItem)
            );
        #endregion SelectedItem Styled Avalonia Property

        #region TabStripPlacement Styled Avalonia Property
        public Dock TabStripPlacement
        {
            get { return GetValue(TabStripPlacementProperty); }
            set { SetValue(TabStripPlacementProperty, value); }
        }

        public static readonly StyledProperty<Dock> TabStripPlacementProperty =
            AvaloniaProperty.Register<TabbedDockGroup, Dock>
            (
                nameof(TabStripPlacement),
                Dock.Top
            );
        #endregion TabStripPlacement Styled Avalonia Property


        #region TabOrientation Direct Avalonia Property
        private Orientation _TabOrientation = Orientation.Vertical;

        public static readonly DirectProperty<TabbedDockGroup, Orientation> TabOrientationProperty =
            AvaloniaProperty.RegisterDirect<TabbedDockGroup, Orientation>
            (
                nameof(TabOrientation),
                o => o.TabOrientation,
                (o, v) => o.TabOrientation = v
            );

        public Orientation TabOrientation
        {
            get => _TabOrientation;
            private set
            {
                SetAndRaise(TabOrientationProperty, ref _TabOrientation, value);
            }
        }

        #endregion TabOrientation Direct Avalonia Property


        #region HorizontalContentAlignment Styled Avalonia Property
        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            AvaloniaProperty.Register<TabbedDockGroup, HorizontalAlignment>
            (
                nameof(HorizontalContentAlignment)
            );
        #endregion HorizontalContentAlignment Styled Avalonia Property


        #region VerticalContentAlignment Styled Avalonia Property
        public VerticalAlignment VerticalContentAlignment
        {
            get { return GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            AvaloniaProperty.Register<TabbedDockGroup, VerticalAlignment>
            (
                nameof(VerticalContentAlignment)
            );
        #endregion VerticalContentAlignment Styled Avalonia Property


        #region ShowCompass Styled Avalonia Property
        public bool ShowCompass
        {
            get { return GetValue(ShowCompassProperty); }
            set { SetValue(ShowCompassProperty, value); }
        }

        public static readonly StyledProperty<bool> ShowCompassProperty =
            AvaloniaProperty.Register<TabbedDockGroup, bool>
            (
                nameof(ShowCompass)
            );
        #endregion ShowCompass Styled Avalonia Property

        #region AllowVerticalDocking Styled Avalonia Property
        public bool AllowVerticalDocking
        {
            get { return GetValue(AllowVerticalDockingProperty); }
            set { SetValue(AllowVerticalDockingProperty, value); }
        }

        public static readonly StyledProperty<bool> AllowVerticalDockingProperty =
            AvaloniaProperty.Register<TabbedDockGroup, bool>
            (
                nameof(AllowVerticalDocking),
                true
            );
        #endregion AllowVerticalDocking Styled Avalonia Property


        #region AllowHorizontalDocking Styled Avalonia Property
        public bool AllowHorizontalDocking
        {
            get { return GetValue(AllowHorizontalDockingProperty); }
            set { SetValue(AllowHorizontalDockingProperty, value); }
        }

        public static readonly StyledProperty<bool> AllowHorizontalDockingProperty =
            AvaloniaProperty.Register<TabbedDockGroup, bool>
            (
                nameof(AllowHorizontalDocking),
                true
            );
        #endregion AllowHorizontalDocking Styled Avalonia Property


        #region DefaultDockOrderInGroup Styled Avalonia Property
        public double? DefaultDockOrderInGroup
        {
            get { return GetValue(DefaultDockOrderInGroupProperty); }
            set { SetValue(DefaultDockOrderInGroupProperty, value); }
        }

        public static readonly StyledProperty<double?> DefaultDockOrderInGroupProperty =
            AvaloniaProperty.Register<TabbedDockGroup, double?>
            (
                nameof(DefaultDockOrderInGroup)
            );
        #endregion DefaultDockOrderInGroup Styled Avalonia Property


        IDisposable? _behavior;
        private void SetBehavior()
        {
            if (Items != null)
            {
                _setItemsBehavior = new SetDockGroupBehavior(this, Items!);
            }

            _mimicCollectionBehavior.InputCollection = Items;
            _singleSelectionBehavior.TheCollection = _mimicCollectionBehavior.OutputCollection;

            _behavior = Items?.AddBehavior(OnItemAdded, OnItemRemoved);
        }


        private void OnItemAdded(IDockGroup child)
        {
            this.SetIsDockVisible();
            child.IsDockVisibleChangedEvent += OnChildDockVisibleChanged;
        }

        private void OnChildDockVisibleChanged(IDockGroup child)
        {
            if (child is DockItem dockItemChild)
            {
                if (!dockItemChild.GetIsDockVisible() && dockItemChild.IsSelected)
                {
                    dockItemChild.IsSelected = false;
                }

                SelectFirstVisibleChild();
            }

            this.SetIsDockVisible();
        }

        private void SelectFirstVisibleChild()
        {
            DockItem? firstVisibleChild =
                Items?.OfType<DockItem>()?.FirstOrDefault(item => item.GetIsDockVisible());

            if (firstVisibleChild != null)
            {
                firstVisibleChild.IsSelected = true;
            }
        }

        private void OnItemRemoved(IDockGroup child)
        {
            if (child is DockItem dockItemChild && dockItemChild.IsSelected)
            {
                dockItemChild.IsSelected = false;
                SelectFirstVisibleChild();
            }

            child.IsDockVisibleChangedEvent -= OnChildDockVisibleChanged;
            child.TheDockManager = null;
            this.SetIsDockVisible();
        }

        private void DisposeBehavior()
        {
            _setItemsBehavior?.Dispose();

            _setItemsBehavior = null;

            _singleSelectionBehavior.TheCollection = null;

            _mimicCollectionBehavior.InputCollection = null;
        }


        public IEnumerable<DockItem> LeafItems => Items.NullToEmpty().Cast<DockItem>().ToList();

        public IDockGroup? GetContainingGroup() => this;

        public bool AutoDestroy
        {
            get; set;
        } = true;

        void IDockGroup.SimplifySelf()
        {
            this.SimplifySelfImpl();
        }

        #region TabSeparatorBackground Styled Avalonia Property
        public IBrush TabSeparatorBackground
        {
            get { return GetValue(TabSeparatorBackgroundProperty); }
            set { SetValue(TabSeparatorBackgroundProperty, value); }
        }

        public static readonly StyledProperty<IBrush> TabSeparatorBackgroundProperty =
            AvaloniaProperty.Register<TabbedDockGroup, IBrush>
            (
                nameof(TabSeparatorBackground)
            );
        #endregion TabSeparatorBackground Styled Avalonia Property


        public IDockGroup CloneIfStable()
        {
            if (IsStableGroup)
            {
                TabbedDockGroup result = new TabbedDockGroup();
                result.AutoDestroy = this.AutoDestroy;
                result.TabOrientation = this.TabOrientation;

                foreach (IDockGroup childGroup in this.DockChildren.NullToEmpty().ToList())
                {
                    result?.DockChildren?.Add(childGroup.CloneIfStable());
                }

                result.TheDockManager = this.TheDockManager;
                return result;
            }
            else
            {
                this.RemoveItselfFromParent();
                return this;
            }
        }
    }
}
