using System.Collections.Generic;
using Microsoft.UI.Input.Inking;

namespace ColoringBook.UndoRedoOperations
{
    /// <summary>
    /// Base class for all undo/redo operations.
    /// 
    /// MIGRATION NOTE: This is pure C# logic — no UWP/WinUI dependencies except
    /// the InkStroke type, which moved from Windows.UI.Input.Inking to
    /// Microsoft.UI.Input.Inking in WinUI 3.
    /// </summary>
    public abstract class UndoRedoOperation
    {
        public abstract OperationType Type { get; }
        public abstract IReadOnlyList<InkStroke> AffectedStrokes { get; }
    }

    public enum OperationType
    {
        AddStrokes,
        EraseStrokes,
        FillCell,
        EraseCell,
        EraseAllStrokes,
        EraseAllCells
    }

    public class AddStrokesOperation : UndoRedoOperation
    {
        private readonly List<InkStroke> _strokes;

        public AddStrokesOperation(IReadOnlyList<InkStroke> strokes)
        {
            _strokes = new List<InkStroke>(strokes);
        }

        public override OperationType Type => OperationType.AddStrokes;
        public override IReadOnlyList<InkStroke> AffectedStrokes => _strokes;
    }

    public class EraseStrokesOperation : UndoRedoOperation
    {
        private readonly List<InkStroke> _strokes;

        public EraseStrokesOperation(IReadOnlyList<InkStroke> strokes)
        {
            _strokes = new List<InkStroke>(strokes);
        }

        public override OperationType Type => OperationType.EraseStrokes;
        public override IReadOnlyList<InkStroke> AffectedStrokes => _strokes;
    }

    /// <summary>
    /// Manages the undo/redo operation stack.
    /// Pure C# — no platform dependencies.
    /// </summary>
    public class UndoRedoManager
    {
        private readonly Stack<UndoRedoOperation> _undoStack = new();
        private readonly Stack<UndoRedoOperation> _redoStack = new();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void AddOperation(UndoRedoOperation operation)
        {
            _undoStack.Push(operation);
            _redoStack.Clear(); // New operations invalidate redo history
        }

        public UndoRedoOperation? Undo()
        {
            if (!CanUndo) return null;

            var op = _undoStack.Pop();
            _redoStack.Push(op);
            return op;
        }

        public UndoRedoOperation? Redo()
        {
            if (!CanRedo) return null;

            var op = _redoStack.Pop();
            _undoStack.Push(op);
            return op;
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }
}
