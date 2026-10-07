using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MATRIX.Core;

namespace MATRIX.App.Views
{
    public partial class GraphCanvasView : UserControl
    {
        public GraphCanvasView()
        {
            InitializeComponent();
        }

        /// <summary>Draw graph: nodes as circles, edges as lines with labels.</summary>
        public void DrawGraph(IReadOnlyList<Node> nodes, IReadOnlyList<Edge> edges, string? selectedNodeId = null)
        {
            GraphCanvas.Children.Clear();
            EmptyState.Visibility = nodes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (nodes.Count == 0) return;

            const double nodeRadius = 30;
            const double minSpacing = 120;
            var canvasWidth = Math.Max(600, GraphCanvas.ActualWidth);
            var canvasHeight = Math.Max(400, GraphCanvas.ActualHeight);

            // Simple circular layout
            var positions = new Dictionary<string, Point>();
            var count = nodes.Count;
            var centerX = canvasWidth / 2;
            var centerY = canvasHeight / 2;
            var radius = Math.Min(canvasWidth, canvasHeight) / 2 - nodeRadius - 20;
            if (count == 1)
            {
                positions[nodes[0].Id] = new Point(centerX, centerY);
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var angle = 2 * Math.PI * i / count - Math.PI / 2;
                    var x = centerX + radius * Math.Cos(angle);
                    var y = centerY + radius * Math.Sin(angle);
                    positions[nodes[i].Id] = new Point(x, y);
                }
            }

            // Draw edges
            var edgeBrush = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
            var labelBrush = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40));
            foreach (var edge in edges)
            {
                if (!positions.ContainsKey(edge.From) || !positions.ContainsKey(edge.To))
                    continue;

                var from = positions[edge.From];
                var to = positions[edge.To];
                var line = new Line
                {
                    X1 = from.X, Y1 = from.Y,
                    X2 = to.X, Y2 = to.Y,
                    Stroke = edgeBrush,
                    StrokeThickness = edge.Criticality == Criticality.Critical ? 3 :
                                       edge.Criticality == Criticality.High ? 2 : 1
                };
                GraphCanvas.Children.Add(line);

                // Edge label
                var midX = (from.X + to.X) / 2;
                var midY = (from.Y + to.Y) / 2;
                var label = new TextBlock
                {
                    Text = SecurityPolicy.ToJsonString(edge.Kind),
                    FontSize = 10,
                    Foreground = labelBrush
                };
                Canvas.SetLeft(label, midX - 30);
                Canvas.SetTop(label, midY - 10);
                GraphCanvas.Children.Add(label);
            }

            // Draw nodes
            foreach (var node in nodes)
            {
                if (!positions.ContainsKey(node.Id)) continue;
                var pos = positions[node.Id];

                var isProject = node.Type == NodeType.Project;
                var isSelected = node.Id == selectedNodeId;
                var fillBrush = isProject
                    ? new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7))
                    : isSelected
                        ? new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07))
                        : new SolidColorBrush(Color.FromRgb(0x66, 0xBB, 0x6A));
                var strokeBrush = isSelected
                    ? new SolidColorBrush(Color.FromRgb(0xFF, 0x57, 0x22))
                    : new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));

                var circle = new Ellipse
                {
                    Width = nodeRadius * 2,
                    Height = nodeRadius * 2,
                    Fill = fillBrush,
                    Stroke = strokeBrush,
                    StrokeThickness = isSelected ? 3 : 1.5
                };
                Canvas.SetLeft(circle, pos.X - nodeRadius);
                Canvas.SetTop(circle, pos.Y - nodeRadius);
                GraphCanvas.Children.Add(circle);

                // Node label
                var label = new TextBlock
                {
                    Text = node.Name,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetLeft(label, pos.X - nodeRadius);
                Canvas.SetTop(label, pos.Y - 8);
                Canvas.SetZIndex(label, 10);
                GraphCanvas.Children.Add(label);

                // Node type label
                var typeLabel = new TextBlock
                {
                    Text = SecurityPolicy.ToJsonString(node.Type),
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66))
                };
                Canvas.SetLeft(typeLabel, pos.X - nodeRadius);
                Canvas.SetTop(typeLabel, pos.Y + nodeRadius + 2);
                GraphCanvas.Children.Add(typeLabel);
            }
        }
    }
}
