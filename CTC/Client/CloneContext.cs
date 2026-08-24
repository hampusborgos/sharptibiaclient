using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    /// <summary>
    /// Carries the original-to-copy map for one clone pass.
    ///
    /// The client state is not a tree. The same ClientCreature is reachable from
    /// ClientViewport.Creatures, from the tile it stands on, and from the
    /// parser's own KnownCreatures cache, and Viewport.Player is literally the
    /// same object as Creatures[PlayerID]. Cloning each of those independently
    /// would produce a state that looks right and then drifts, because an update
    /// arriving through one route would not be seen through the others.
    ///
    /// So every clone goes through here, and anything reached twice comes out as
    /// one object on the far side.
    ///
    /// Two things are deliberately never cloned. ItemType is an immutable
    /// singleton owned by TibiaGameData that reaches a live Texture2D through
    /// its GameSprite. ClientMessage is never mutated after it is constructed.
    /// Both are shared by reference.
    /// </summary>
    public class CloneContext
    {
        private readonly Dictionary<object, object> Copies =
            new Dictionary<object, object>(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// Adopted objects that still need their fields written.
        /// </summary>
        private readonly HashSet<object> Unpopulated =
            new HashSet<object>(ReferenceEqualityComparer.Instance);

        public delegate T Allocator<T>();
        public delegate void Populator<T>(T From, T To);

        /// <summary>
        /// The copy of Original, made once however many times it is reached.
        ///
        /// The copy is registered before it is populated, so a cycle would
        /// terminate rather than recurse forever.
        /// </summary>
        public T Get<T>(T Original, Allocator<T> Allocate, Populator<T> Populate)
            where T : class
        {
            if (Original == null)
                return null;

            object Existing;
            if (Copies.TryGetValue(Original, out Existing))
            {
                // An adopted object is in the map before it holds any of the
                // original's values, so the first reach still has to fill it in.
                if (Unpopulated.Remove(Existing))
                    Populate(Original, (T)Existing);
                return (T)Existing;
            }

            T Copy = Allocate();
            Copies[Original] = Copy;
            Populate(Original, Copy);
            return Copy;
        }

        /// <summary>
        /// Makes Original clone *into* an object that already exists, rather than
        /// into a fresh one.
        ///
        /// This is how a restore keeps object identity across a seek: the panels
        /// hold references to the live ClientPlayer and its ClientSkill objects
        /// and read them every frame, so the values have to land in the instances
        /// they are already holding.
        /// </summary>
        public void Adopt<T>(T Original, T Existing) where T : class
        {
            if (Original == null || Existing == null)
                return;

            Copies[Original] = Existing;
            Unpopulated.Add(Existing);
        }

        public Boolean Has(object Original)
        {
            return Original != null && Copies.ContainsKey(Original);
        }
    }
}
