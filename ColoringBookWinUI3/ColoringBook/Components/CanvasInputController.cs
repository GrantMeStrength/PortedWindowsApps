using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Input.Inking;
using Microsoft.UI.Xaml.Controls;
using ColoringBook.ViewModels;
using ColoringBook.UndoRedoOperations;

namespace ColoringBook.Components
{
    /// <summary>
    /// Manages InkCanvas input and coordinates with the ViewModel for undo/redo.
    /// 
    /// MIGRATION NOTES:
    /// 1. InkCanvas in WinUI 3 is Microsoft.UI.Xaml.Controls.InkCanvas (same API surface)
    /// 2. InkPresenter is accessed the same way: inkCanvas.InkPresenter
    /// 3. StrokesCollected/StrokesErased events work identically
    /// 4. Key difference: UWP used CoreInputDeviceTypes from Windows.UI.Core;
    ///    WinUI 3 uses Microsoft.UI.Input.Inking.InkInputConfiguration
    /// 5. InkToolbar: WinUI 3 has InkToolbar in Microsoft.UI.Xaml.Controls but
    ///    the UWP custom toolbar was heavily customized — we build a custom XAML toolbar instead
    /// </summary>
    public class CanvasInputController
    {
        private readonly InkCanvas _inkCanvas;
        private readonly ColoringViewModel _viewModel;
        private InkStrokeContainer _strokeContainer;

        public CanvasInputController(InkCanvas inkCanvas, ColoringViewModel viewModel)
        {
            _inkCanvas = inkCanvas;
            _viewModel = viewModel;
            _strokeContainer = _inkCanvas.InkPresenter.StrokeContainer;

            SetupInkPresenter();
        }

        public InkStrokeContainer StrokeContainer => _strokeContainer;

        private void SetupInkPresenter()
        {
            var presenter = _inkCanvas.InkPresenter;

            // Accept all input types (mouse, touch, pen)
            presenter.InputDeviceTypes =
                Microsoft.UI.Core.CoreInputDeviceTypes.Mouse |
                Microsoft.UI.Core.CoreInputDeviceTypes.Pen |
                Microsoft.UI.Core.CoreInputDeviceTypes.Touch;

            // Set initial drawing attributes
            presenter.UpdateDefaultDrawingAttributes(_viewModel.GetCurrentDrawingAttributes());

            // Wire up stroke events for undo/redo
            presenter.StrokesCollected += OnStrokesCollected;
            presenter.StrokesErased += OnStrokesErased;
        }

        public void UpdateDrawingAttributes()
        {
            var attrs = _viewModel.GetCurrentDrawingAttributes();
            _inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(attrs);

            // Switch between draw and erase modes
            if (_viewModel.CurrentTool == Models.DrawingTool.Eraser)
            {
                _inkCanvas.InkPresenter.InputProcessingConfiguration.Mode =
                    InkInputProcessingMode.Erasing;
            }
            else
            {
                _inkCanvas.InkPresenter.InputProcessingConfiguration.Mode =
                    InkInputProcessingMode.Inking;
            }
        }

        /// <summary>
        /// Applies an undo/redo operation to the ink canvas.
        /// 
        /// MIGRATION NOTE: InkStroke manipulation APIs are identical between UWP and WinUI 3.
        /// The key difference is the namespace (Microsoft.UI.Input.Inking).
        /// </summary>
        public void ApplyUndoRedo(UndoRedoOperation? operation, bool isUndo)
        {
            if (operation == null) return;

            switch (operation.Type)
            {
                case OperationType.AddStrokes when isUndo:
                    // Undo add = remove the strokes
                    foreach (var stroke in operation.AffectedStrokes)
                    {
                        var match = _strokeContainer.GetStrokes()
                            .FirstOrDefault(s => s.Id == stroke.Id);
                        if (match != null)
                        {
                            match.Selected = true;
                        }
                    }
                    _strokeContainer.DeleteSelected();
                    break;

                case OperationType.EraseStrokes when isUndo:
                    // Undo erase = re-add the strokes
                    foreach (var stroke in operation.AffectedStrokes)
                    {
                        _strokeContainer.AddStroke(stroke.Clone());
                    }
                    break;

                case OperationType.AddStrokes when !isUndo:
                    // Redo add = re-add the strokes
                    foreach (var stroke in operation.AffectedStrokes)
                    {
                        _strokeContainer.AddStroke(stroke.Clone());
                    }
                    break;

                case OperationType.EraseStrokes when !isUndo:
                    // Redo erase = remove again
                    foreach (var stroke in operation.AffectedStrokes)
                    {
                        var match = _strokeContainer.GetStrokes()
                            .FirstOrDefault(s => s.Id == stroke.Id);
                        if (match != null)
                        {
                            match.Selected = true;
                        }
                    }
                    _strokeContainer.DeleteSelected();
                    break;
            }
        }

        public IReadOnlyList<InkStroke> GetAllStrokes()
        {
            return _strokeContainer.GetStrokes();
        }

        public void ClearAll()
        {
            _strokeContainer.Clear();
        }

        private void OnStrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            _viewModel.RecordStrokeAdded(args.Strokes.ToList());
        }

        private void OnStrokesErased(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            _viewModel.RecordStrokesErased(args.Strokes.ToList());
        }
    }
}
