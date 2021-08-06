﻿using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;
using NP.Concepts.Behaviors;
using System;

namespace DockWindowsSample
{
    public class DockTabbedGroup : TemplatedControl
    {
        IDisposable? _behavior;

        private AvaloniaList<DockItem> _items = new AvaloniaList<DockItem>();

        SingleSelectionFirstByDefaultBehavior<DockItem> _singleSelectionBehavior =
            new SingleSelectionFirstByDefaultBehavior<DockItem>();

        public DockTabbedGroup()
        {
            SetBehavior();

            _singleSelectionBehavior.PropertyChanged += _singleSelectionBehavior_PropertyChanged;

            SetSelectedItem();
        }

        private void _singleSelectionBehavior_PropertyChanged
        (
            object? sender, 
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            SetSelectedItem();
        }

        private void SetSelectedItem()
        {
            SelectedItem = _singleSelectionBehavior.TheSelectedItem;
        }

        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly DirectProperty<DockTabbedGroup, AvaloniaList<DockItem>> ItemsProperty =
            AvaloniaProperty.RegisterDirect<DockTabbedGroup, AvaloniaList<DockItem>>
            (
                nameof(Items), 
                o => o.Items, 
                (o, v) => o.Items = v);

        /// <summary>
        /// Gets or sets the items to display.
        /// </summary>
        [Content]
        public AvaloniaList<DockItem> Items
        {
            get 
            { 
                return _items; 
            }

            set 
            {
                DisposeBehavior();

                SetAndRaise(ItemsProperty, ref _items, value);

                SetBehavior();
            }
        }


        #region SelectedItem Styled Avalonia Property
        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            private set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly StyledProperty<object> SelectedItemProperty =
            AvaloniaProperty.Register<DockTabbedGroup, object>
            (
                nameof(SelectedItem)
            );
        #endregion SelectedItem Styled Avalonia Property


        private void SetBehavior()
        {
            _behavior = _items?.AddBehavior(OnItemAdded, OnItemRemoved);
            _singleSelectionBehavior.TheCollection = _items;
        }

        private void DisposeBehavior()
        {
            _behavior?.Dispose();

            _behavior = null;

            _singleSelectionBehavior.TheCollection = null; 
        }

        private void OnItemAdded(DockItem item)
        {
            item.RemoveEvent += Item_RemoveEvent;
        }

        private void Item_RemoveEvent(IRemovable item)
        {
            this.Items.Remove((DockItem) item);
        }

        private void OnItemRemoved(DockItem item)
        {
            item.RemoveEvent -= Item_RemoveEvent;
        }
    }
}
