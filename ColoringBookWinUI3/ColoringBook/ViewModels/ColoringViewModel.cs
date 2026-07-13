using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ColoringBook.Models;
using ColoringBook.UndoRedoOperations;
using Microsoft.UI;
using Windows.UI.Input.Inking;
using Windows.UI;

namespace ColoringBook.ViewModels
{
    /// <summary>
    /// ViewModel for the coloring page — manages tool state, color palette, undo/redo.
    /// 
    /// MIGRATION NOTES:
    /// - Color type: WinUI 3 uses Windows.UI.Color (same as UWP), NOT Microsoft.UI.Color
    ///   for ink stroke attributes. This is a common confusion point.
    /// - InkDrawingAttributes stays in Windows.UI.Input.Inking (same as UWP — NOT Microsoft.UI.Input.Inking)
    /// - Undo/Redo system is pure C# logic — ports unchanged
    /// </summary>
    public partial class ColoringViewModel : ObservableObject
    {
        private readonly UndoRedoManager _undoRedoManager = new();

        [ObservableProperty]
        private bool _canUndo;

        [ObservableProperty]
        private bool _canRedo;

        [ObservableProperty]
        private DrawingTool _currentTool = DrawingTool.Pen;

        [ObservableProperty]
        private Color _currentColor = Colors.Red;

        [ObservableProperty]
        private double _strokeSize = 4.0;

        [ObservableProperty]
        private double _opacity = 1.0;

        [ObservableProperty]
        private bool _isFillMode;

        [ObservableProperty]
        private string _coloringTitle = "Untitled";

        [ObservableProperty]
        private bool _hasUnsavedChanges;

        /// <summary>
        /// Predefined color palette for the coloring toolbar.
        /// </summary>
        public IReadOnlyList<Color> ColorPalette { get; } = new[]
        {
            Colors.Red, Colors.OrangeRed, Colors.Orange, Colors.Gold,
            Colors.Yellow, Colors.GreenYellow, Colors.Green, Colors.DarkGreen,
            Colors.Teal, Colors.Cyan, Colors.DeepSkyBlue, Colors.Blue,
            Colors.DarkBlue, Colors.Purple, Colors.MediumPurple, Colors.DeepPink,
            Colors.HotPink, Colors.Brown, Colors.SaddleBrown, Colors.Chocolate,
            Colors.Black, Colors.DarkGray, Colors.Gray, Colors.White
        };

        public IReadOnlyList<double> StrokeSizes { get; } = new[] { 2.0, 4.0, 8.0, 12.0, 16.0, 24.0 };

        [RelayCommand]
        private void SelectTool(string toolName)
        {
            if (Enum.TryParse<DrawingTool>(toolName, out var tool))
            {
                CurrentTool = tool;
                IsFillMode = tool == DrawingTool.Fill;
            }
        }

        [RelayCommand]
        private void SelectColor(Color color)
        {
            CurrentColor = color;
        }

        /// <summary>
        /// Gets the InkDrawingAttributes for the current tool configuration.
        /// 
        /// MIGRATION NOTE: InkDrawingAttributes remains in Windows.UI.Input.Inking in WinUI 3
        /// desktop apps — contrary to early documentation suggesting Microsoft.UI.Input.Inking.
        /// </summary>
        public InkDrawingAttributes GetCurrentDrawingAttributes()
        {
            var attrs = new InkDrawingAttributes
            {
                Color = CurrentColor,
                Size = new Windows.Foundation.Size(StrokeSize, StrokeSize),
                PenTip = CurrentTool == DrawingTool.Calligraphy
                    ? PenTipShape.Rectangle
                    : PenTipShape.Circle,
                IsHighlighter = false,
            };

            if (CurrentTool == DrawingTool.Pencil)
            {
                attrs.PenTipTransform = System.Numerics.Matrix3x2.CreateRotation((float)(Math.PI / 6));
            }

            return attrs;
        }

        [RelayCommand]
        public void RecordStrokeAdded(IReadOnlyList<InkStroke> strokes)
        {
            _undoRedoManager.AddOperation(new AddStrokesOperation(strokes));
            UpdateUndoRedoState();
            HasUnsavedChanges = true;
        }

        [RelayCommand]
        public void RecordStrokesErased(IReadOnlyList<InkStroke> strokes)
        {
            _undoRedoManager.AddOperation(new EraseStrokesOperation(strokes));
            UpdateUndoRedoState();
            HasUnsavedChanges = true;
        }

        public UndoRedoOperation? Undo()
        {
            var op = _undoRedoManager.Undo();
            UpdateUndoRedoState();
            HasUnsavedChanges = true;
            return op;
        }

        public UndoRedoOperation? Redo()
        {
            var op = _undoRedoManager.Redo();
            UpdateUndoRedoState();
            HasUnsavedChanges = true;
            return op;
        }

        private void UpdateUndoRedoState()
        {
            CanUndo = _undoRedoManager.CanUndo;
            CanRedo = _undoRedoManager.CanRedo;
        }

        private async Task ExportAsync(
            string backgroundImagePath,
            IReadOnlyList<InkStroke> strokes,
            int width, int height)
        {
            await ColoringExporter.ExportToPngAsync(backgroundImagePath, strokes, width, height);
        }
    }
}
