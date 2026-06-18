using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NAudio.Vst3;

/// <summary>
/// Read-only collection of every parameter exposed by a <see cref="Vst3Plugin"/>'s
/// <c>IEditController</c>, in declaration order.
/// </summary>
/// <remarks>
/// <para>
/// Built once at plug-in instantiation by walking <c>IEditController::getParameterCount</c> /
/// <c>getParameterInfo</c>. The collection itself is immutable; individual
/// <see cref="Vst3Parameter"/> values are live (each property read round-trips to the
/// controller).
/// </para>
/// </remarks>
public sealed class Vst3ParameterCollection : IReadOnlyList<Vst3Parameter>
{
    private readonly List<Vst3Parameter> _ordered;
    private readonly Dictionary<uint, Vst3Parameter> _byId;

    internal Vst3ParameterCollection(IList<Vst3Parameter> parameters)
    {
        _ordered = new List<Vst3Parameter>(parameters);
        _byId = new Dictionary<uint, Vst3Parameter>(_ordered.Count);
        foreach (var p in _ordered)
        {
            // Plug-ins should give every parameter a unique id, but a few in the wild duplicate;
            // keep the first occurrence so ordinal indexing stays consistent.
            _byId.TryAdd(p.Id, p);
        }
        BypassParameter = _ordered.FirstOrDefault(p => p.IsBypass);
    }

    /// <inheritdoc/>
    public int Count => _ordered.Count;

    /// <inheritdoc/>
    public Vst3Parameter this[int index] => _ordered[index];

    /// <summary>
    /// The bypass parameter, if the plug-in declared one with
    /// <see cref="Vst3ParameterFlags.IsBypass"/>. Most hosts surface bypass as a switch separate
    /// from the rest of the parameter list.
    /// </summary>
    public Vst3Parameter? BypassParameter { get; }

    /// <summary>O(1) lookup by VST 3 parameter id.</summary>
    /// <exception cref="KeyNotFoundException">When no parameter has the requested id.</exception>
    public Vst3Parameter GetById(uint id) => _byId[id];

    /// <summary>Tries to find a parameter by id without throwing.</summary>
    public bool TryGetById(uint id, out Vst3Parameter parameter)
        => _byId.TryGetValue(id, out parameter!);

    /// <summary>
    /// Case-insensitive title lookup; returns <c>null</c> when no match is found. Titles are
    /// NOT guaranteed unique across a plug-in — for unambiguous addressing prefer
    /// <see cref="GetById"/>.
    /// </summary>
    public Vst3Parameter? FindByTitle(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        foreach (var p in _ordered)
        {
            if (string.Equals(p.Title, title, StringComparison.OrdinalIgnoreCase))
            {
                return p;
            }
        }
        return null;
    }

    /// <inheritdoc/>
    public IEnumerator<Vst3Parameter> GetEnumerator() => _ordered.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
