using System.Collections.Generic;
using UnityEngine;

/// <summary>A note that was put down on a NotePlaceSurface. Added when it is placed, removed when it is picked up.</summary>
public class PlacedNote : MonoBehaviour
{
    /// <summary>Every note currently lying on a surface.</summary>
    public static readonly List<PlacedNote> All = new List<PlacedNote>();

    public NotePlaceSurface surface;
    public float surfaceY;
    /// <summary>The tower this note is part of, null while it lies on its own.</summary>
    public NotePile pile;

    /// <summary>Only the top note of a tower can be picked up.</summary>
    public bool IsPickable => pile == null || pile.Top == this;

    void OnEnable()
    {
        if (!All.Contains(this))
        {
            All.Add(this);
        }
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    /// <summary>Called when the note leaves the surface.</summary>
    public void OnPickedUp()
    {
        if (pile != null)
        {
            pile.Remove(this);
        }
        pile = null;
        Destroy(this);
    }
}

/// <summary>Notes stacked on top of each other, bottom first.</summary>
public class NotePile
{
    public readonly List<PlacedNote> notes = new List<PlacedNote>();

    public PlacedNote Top => notes.Count > 0 ? notes[notes.Count - 1] : null;

    public void Add(PlacedNote _note)
    {
        notes.Add(_note);
        _note.pile = this;
    }

    public void Remove(PlacedNote _note)
    {
        notes.Remove(_note);
        // a single note is not a tower anymore
        if (notes.Count <= 1)
        {
            for (int i = 0; i < notes.Count; i++)
            {
                notes[i].pile = null;
            }
            notes.Clear();
        }
    }
}
