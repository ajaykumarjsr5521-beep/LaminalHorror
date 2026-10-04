using System;
using UnityEngine;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Puzzle
{
    /// <summary>
    /// In-world code lock. Interacting requests the keypad UI (F-07 subscribes to KeypadRequested);
    /// digits are fed through Press/Submit. Solving it unlocks the target door.
    /// </summary>
    public class CodeLock : MonoBehaviour, IInteractable
    {
        [Tooltip("Digits only. Validated on startup; an invalid code disables the lock and logs an error.")]
        public string Code = "";
        public Door TargetDoor;

        public event Action<CodeLock> KeypadRequested;
        public event Action<string> EntryChanged;
        public event Action Rejected;
        public event Action Solved;

        CodeLockModel _model;

        public bool IsConfigured => _model != null;
        public bool IsSolved => _model != null && _model.IsSolved;
        public string Entry => _model?.Entry ?? "";
        public int CodeLength => _model?.Length ?? 0;

        public string Prompt => !IsConfigured ? "" : (IsSolved ? "Unlocked" : "Use keypad");

        void Awake()
        {
            var problem = CodeLockModel.ValidateCode(Code);
            if (problem != null)
            {
                Debug.LogError($"CodeLock '{name}' disabled: {problem}", this);
                return;
            }
            _model = new CodeLockModel(Code);
        }

        public void Interact(Interactor interactor)
        {
            if (IsConfigured && !IsSolved) KeypadRequested?.Invoke(this);
        }

        public void Press(char digit)
        {
            if (_model != null && _model.EnterDigit(digit)) EntryChanged?.Invoke(_model.Entry);
        }

        public void Backspace()
        {
            if (_model == null) return;
            _model.Backspace();
            EntryChanged?.Invoke(_model.Entry);
        }

        public SubmitResult Submit()
        {
            if (_model == null) return SubmitResult.Incomplete;
            var result = _model.Submit();
            if (result == SubmitResult.Wrong) { EntryChanged?.Invoke(_model.Entry); Rejected?.Invoke(); }
            else if (result == SubmitResult.Correct)
            {
                if (TargetDoor != null) TargetDoor.Unlock();
                Solved?.Invoke();
            }
            return result;
        }

        /// <summary>Save/load hook; persistence itself is F-06.</summary>
        public void RestoreSolved(bool solved)
        {
            if (_model == null) return;
            _model.RestoreSolved(solved);
            if (solved && TargetDoor != null) TargetDoor.Unlock();
        }
    }
}
