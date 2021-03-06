﻿using System.Windows;
using System.Windows.Controls.Primitives;

using Visualization.Controls.CirclePacking;
using Visualization.Controls.Data;
using Visualization.Controls.Drawing;
using Visualization.Controls.Interfaces;

namespace Visualization.Controls
{
    /// <summary>
    /// Interaction logic for CirclePackingView.xaml
    /// </summary>
    public sealed partial class CirclePackingView : HierarchicalDataViewBase
    {
        public CirclePackingView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        protected override void ClosePopup()
        {
            _popup.IsOpen = false;
        }

        protected override IRenderer CreateRenderer()
        {
            return new CirclePackingRenderer(_colorScheme);
        }

        protected override DrawingCanvas GetCanvas()
        {
            return _canvasOrImage;
        }


        protected override void InitPopup(HierarchicalData hit)
        {
            _popupText.Text = hit.Description;

            _popup.PlacementTarget = GetCanvas();
            _popup.Placement = PlacementMode.Mouse;
            _popup.Visibility = Visibility.Visible;
            _popup.IsOpen = true;
        }


        private void CirclePacking_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            HideToolView();
        }
    }
}