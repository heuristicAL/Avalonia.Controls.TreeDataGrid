﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    public class HierarchicalRow<TModel> : ExpanderRowBase<TModel>, IDisposable
    {
        private readonly IExpanderRowController<TModel> _controller;
        private Comparison<TModel>? _comparison;
        private IEnumerable<TModel>? _childModels;
        private ChildRows? _childRows;
        private bool _isExpanded;

        public HierarchicalRow(
            IExpanderRowController<TModel> controller,
            IndexPath index,
            TModel model,
            Comparison<TModel>? comparison)
        {
            _controller = controller;
            _comparison = comparison;
            Index = index;
            Model = model;
        }

        public IReadOnlyList<HierarchicalRow<TModel>>? Children => _isExpanded ? _childRows : null;
        public override object? Header => Index;
        public IndexPath Index { get; }
        public override bool IsExpanded => _isExpanded;
        public override TModel Model { get; }

        public override GridLength Height 
        {
            get => GridLength.Auto;
            set { }
        }

        public void Dispose() => _childRows?.Dispose();

        public override void Expand(IEnumerable<TModel> childModels)
        {
            if (_isExpanded)
                throw new InvalidOperationException("Row is already expanded.");

            if (_childModels != childModels)
            {
                _childModels = childModels;
                _childRows?.Dispose();
                _childRows = new ChildRows(
                    this,
                    ItemsSourceView<TModel>.GetOrCreate(childModels),
                    _comparison);
            }

            _isExpanded = true;
            _controller.OnChildCollectionChanged(this, CollectionExtensions.ResetEvent);
        }

        public override void Collapse()
        {
            if (!_isExpanded)
                return;

            _isExpanded = false;
            _controller.OnChildCollectionChanged(this, CollectionExtensions.ResetEvent);
        }

        private void OnChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isExpanded)
                _controller.OnChildCollectionChanged(this, e);
        }

        private class ChildRows : SortableRowsBase<TModel, HierarchicalRow<TModel>>,
            IReadOnlyList<HierarchicalRow<TModel>>
        {
            private readonly HierarchicalRow<TModel> _owner;

            public ChildRows(
                HierarchicalRow<TModel> owner,
                ItemsSourceView<TModel> items,
                Comparison<TModel>? comparer)
                : base(items, comparer)
            {
                _owner = owner;
                CollectionChanged += OnCollectionChanged;
            }

            protected override HierarchicalRow<TModel> CreateRow(int modelIndex, TModel model)
            {
                return new HierarchicalRow<TModel>(
                    _owner._controller,
                    _owner.Index.CloneWithChildIndex(modelIndex),
                    model,
                    _owner._comparison);
            }

            private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                _owner._controller.OnChildCollectionChanged(_owner, e);
            }
        }
    }
}