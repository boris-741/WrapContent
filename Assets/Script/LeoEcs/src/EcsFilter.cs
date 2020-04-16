// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2020 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global

namespace Leopotam.EcsNew {
    /// <summary>
    /// Common interface for all filter listeners.
    /// </summary>
    public interface IEcsFilterListener {
        void OnEntityAdded (in EcsEntity entity);
        void OnEntityRemoved (in EcsEntity entity);
    }

    /// <summary>
    /// Container for filtered entities based on specified constraints.
    /// </summary>
#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
#if UNITY_2019_1_OR_NEWER
    [UnityEngine.Scripting.Preserve]
#endif
    public abstract class EcsFilter {
        protected EcsEntity[] Entities = new EcsEntity[EcsHelpers.FilterEntitiesSize];
        protected int EntitiesCount;

        int _lockCount;

        DelayedOp[] _delayedOps = new DelayedOp[EcsHelpers.FilterEntitiesSize];
        int _delayedOpsCount;

        // ReSharper disable MemberCanBePrivate.Global
        protected IEcsFilterListener[] Listeners = new IEcsFilterListener[4];
        protected int ListenersCount;
        // ReSharper restore MemberCanBePrivate.Global

        protected internal int[] IncludedTypeIndices;
        protected internal int[] ExcludedTypeIndices;

        public Type[] IncludedTypes;
        public Type[] ExcludedTypes;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator () {
            return new Enumerator (this);
        }

        /// <summary>
        /// Gets entity by index.
        /// </summary>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref EcsEntity GetEntity (in int idx) {
            return ref Entities[idx];
        }

        /// <summary>
        /// Gets entities count.
        /// </summary>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public int GetEntitiesCount () {
            return EntitiesCount;
        }

        /// <summary>
        /// Is filter not contains entities.
        /// </summary>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool IsEmpty () {
            return EntitiesCount == 0;
        }

        /// <summary>
        /// Subscribes listener to filter events.
        /// </summary>
        /// <param name="listener">Listener.</param>
        public void AddListener (IEcsFilterListener listener) {
#if DEBUG
            for (int i = 0, iMax = ListenersCount; i < iMax; i++) {
                if (Listeners[i] == listener) {
                    throw new Exception ("Listener already subscribed.");
                }
            }
#endif
            if (Listeners.Length == ListenersCount) {
                Array.Resize (ref Listeners, ListenersCount << 1);
            }
            Listeners[ListenersCount++] = listener;
        }

        // ReSharper disable once CommentTypo
        /// <summary>
        /// Unsubscribes listener from filter events.
        /// </summary>
        /// <param name="listener">Listener.</param>
        public void RemoveListener (IEcsFilterListener listener) {
            for (int i = 0, iMax = ListenersCount; i < iMax; i++) {
                if (Listeners[i] == listener) {
                    ListenersCount--;
                    // cant fill gap with last element due listeners order is important.
                    Array.Copy (Listeners, i + 1, Listeners, i, ListenersCount - i);
                    break;
                }
            }
        }

        /// <summary>
        /// Is filter compatible with components on entity with optional added / removed component.
        /// </summary>
        /// <param name="entityData">Entity data.</param>
        /// <param name="addedRemovedTypeIndex">Optional added (greater 0) or removed (less 0) component. Will be ignored if zero.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal bool IsCompatible (in EcsWorld.EcsEntityData entityData, int addedRemovedTypeIndex) {
            var incIdx = IncludedTypeIndices.Length - 1;
            for (; incIdx >= 0; incIdx--) {
                var typeIdx = IncludedTypeIndices[incIdx];
                var idx = entityData.ComponentsCountX2 - 2;
                for (; idx >= 0; idx -= 2) {
                    var typeIdx2 = entityData.Components[idx];
                    if (typeIdx2 == -addedRemovedTypeIndex) {
                        continue;
                    }
                    if (typeIdx2 == addedRemovedTypeIndex || typeIdx2 == typeIdx) {
                        break;
                    }
                }
                // not found.
                if (idx == -2) {
                    break;
                }
            }
            // one of required component not found.
            if (incIdx != -1) {
                return false;
            }
            // check for excluded components.
            if (ExcludedTypeIndices != null) {
                for (var excIdx = 0; excIdx < ExcludedTypeIndices.Length; excIdx++) {
                    var typeIdx = ExcludedTypeIndices[excIdx];
                    for (var idx = entityData.ComponentsCountX2 - 2; idx >= 0; idx -= 2) {
                        var typeIdx2 = entityData.Components[idx];
                        if (typeIdx2 == -addedRemovedTypeIndex) {
                            continue;
                        }
                        if (typeIdx2 == addedRemovedTypeIndex || typeIdx2 == typeIdx) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        protected bool AddDelayedOp (bool isAdd, in EcsEntity entity) {
            if (_lockCount <= 0) {
                return false;
            }
            if (_delayedOps.Length == _delayedOpsCount) {
                Array.Resize (ref _delayedOps, _delayedOpsCount << 1);
            }
            ref var op = ref _delayedOps[_delayedOpsCount++];
            op.IsAdd = isAdd;
            op.Entity = entity;
            return true;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        protected void ProcessListeners (bool isAdd, in EcsEntity entity) {
            if (isAdd) {
                for (int i = 0, iMax = ListenersCount; i < iMax; i++) {
                    Listeners[i].OnEntityAdded (entity);
                }
            } else {
                for (int i = 0, iMax = ListenersCount; i < iMax; i++) {
                    Listeners[i].OnEntityRemoved (entity);
                }
            }
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        void Lock () {
            _lockCount++;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        void Unlock () {
#if DEBUG
            if (_lockCount <= 0) {
                throw new Exception ($"Invalid lock-unlock balance for \"{GetType ().Name}\".");
            }
#endif
            _lockCount--;
            if (_lockCount == 0 && _delayedOpsCount > 0) {
                // process delayed operations.
                for (int i = 0, iMax = _delayedOpsCount; i < iMax; i++) {
                    ref var op = ref _delayedOps[i];
                    if (op.IsAdd) {
                        OnAddEntity (op.Entity);
                    } else {
                        OnRemoveEntity (op.Entity);
                    }
                }
                _delayedOpsCount = 0;
            }
        }

#if DEBUG
        /// <summary>
        /// For debug purposes. Check filters equality by included / excluded components.
        /// </summary>
        /// <param name="filter">Filter to compare.</param>
        internal bool AreComponentsSame (EcsFilter filter) {
            if (IncludedTypeIndices.Length != filter.IncludedTypeIndices.Length) {
                return false;
            }
            for (var i = 0; i < IncludedTypeIndices.Length; i++) {
                if (Array.IndexOf (filter.IncludedTypeIndices, IncludedTypeIndices[i]) == -1) {
                    return false;
                }
            }
            if ((ExcludedTypeIndices == null && filter.ExcludedTypeIndices != null) ||
                (ExcludedTypeIndices != null && filter.ExcludedTypeIndices == null)) {
                return false;
            }
            if (ExcludedTypeIndices != null) {
                if (filter.ExcludedTypeIndices == null || ExcludedTypeIndices.Length != filter.ExcludedTypeIndices.Length) {
                    return false;
                }
                for (var i = 0; i < ExcludedTypeIndices.Length; i++) {
                    if (Array.IndexOf (filter.ExcludedTypeIndices, ExcludedTypeIndices[i]) == -1) {
                        return false;
                    }
                }
            }
            return true;
        }
#endif

        /// <summary>
        /// Event for adding compatible entity to filter.
        /// Warning: Don't call manually!
        /// </summary>
        /// <param name="entity">Entity.</param>
        public abstract void OnAddEntity (in EcsEntity entity);

        /// <summary>
        /// Event for removing non-compatible entity to filter.
        /// Warning: Don't call manually!
        /// </summary>
        /// <param name="entity">Entity.</param>
        public abstract void OnRemoveEntity (in EcsEntity entity);

        public struct Enumerator : IDisposable {
            readonly EcsFilter _filter;
            readonly int _count;
            int _idx;

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            internal Enumerator (EcsFilter filter) {
                _filter = filter;
                _count = _filter.GetEntitiesCount ();
                _idx = -1;
                _filter.Lock ();
            }

            public int Current {
                [MethodImpl (MethodImplOptions.AggressiveInlining)]
                get => _idx;
            }

#if ENABLE_IL2CPP
            [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
            [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public void Dispose () {
                _filter.Unlock ();
            }

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public bool MoveNext () {
                return ++_idx < _count;
            }
        }

        struct DelayedOp {
            public bool IsAdd;
            public EcsEntity Entity;
        }
    }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
#if UNITY_2019_1_OR_NEWER
    [UnityEngine.Scripting.Preserve]
#endif
    public class EcsFilter<Inc1> : EcsFilter where Inc1 : struct {
        int[] _Get1;
        readonly bool _allow1;

        readonly EcsComponentPool<Inc1> _pool1;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref Inc1 Get1 (int idx) {
            return ref _pool1.Items[_Get1[idx]];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsComponentRef<Inc1> Get1Ref (int idx) {
            return _pool1.Ref (_Get1[idx]);
        }

        /// <summary>
        /// Optimizes filtered data for fast access.
        /// </summary>
        public void Optimize () {
            OptimizeSort (0, EntitiesCount - 1);
        }

        void OptimizeSort (int left, int right) {
            if (left < right) {
                var q = OptimizeSortPartition (left, right);
                OptimizeSort (left, q - 1);
                OptimizeSort (q + 1, right);
            }
        }

        int OptimizeSortPartition (int left, int right) {
            var pivot = _Get1[right];
            var pivotE = Entities[right];
            var i = left;
            for (var j = left; j < right; j++) {
                if (_Get1[j] <= pivot) {
                    var c = _Get1[j];
                    _Get1[j] = _Get1[i];
                    _Get1[i] = c;
                    var e = Entities[j];
                    Entities[j] = Entities[i];
                    Entities[i] = e;
                    i++;
                }
            }
            _Get1[right] = _Get1[i];
            _Get1[i] = pivot;
            Entities[right] = Entities[i];
            Entities[i] = pivotE;
            return i;
        }

        protected EcsFilter (EcsWorld world) {
            _allow1 = !EcsComponentType<Inc1>.IsIgnoreInFilter;
            _pool1 = world.GetPool<Inc1> ();
            _Get1 = _allow1 ? new int[EcsHelpers.FilterEntitiesSize] : null;
            IncludedTypeIndices = new[] { EcsComponentType<Inc1>.TypeIndex };
            IncludedTypes = new[] { EcsComponentType<Inc1>.Type };
        }

        /// <summary>
        /// Event for adding compatible entity to filter.
        /// Warning: Don't call manually!
        /// </summary>
        /// <param name="entity">Entity.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void OnAddEntity (in EcsEntity entity) {
            if (AddDelayedOp (true, entity)) { return; }
            if (Entities.Length == EntitiesCount) {
                var newSize = EntitiesCount << 1;
                Array.Resize (ref Entities, newSize);
                if (_allow1) { Array.Resize (ref _Get1, newSize); }
            }
            // inlined and optimized World.GetComponent() call.
            ref var entityData = ref entity.Owner.GetEntityData (entity);
            var allow1 = _allow1;
            for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2) {
                var typeIdx = entityData.Components[i];
                var itemIdx = entityData.Components[i + 1];
                if (allow1 && typeIdx == EcsComponentType<Inc1>.TypeIndex) {
                    _Get1[EntitiesCount] = itemIdx;
                    allow1 = false;
                }
            }
            Entities[EntitiesCount++] = entity;
            ProcessListeners (true, entity);
        }

        /// <summary>
        /// Event for removing non-compatible entity to filter.
        /// Warning: Don't call manually!
        /// </summary>
        /// <param name="entity">Entity.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void OnRemoveEntity (in EcsEntity entity) {
            if (AddDelayedOp (false, entity)) { return; }
            for (int i = 0, iMax = EntitiesCount; i < iMax; i++) {
                if (Entities[i] == entity) {
                    EntitiesCount--;
                    if (i < EntitiesCount) {
                        Entities[i] = Entities[EntitiesCount];
                        if (_allow1) { _Get1[i] = _Get1[EntitiesCount]; }
                    }
                    ProcessListeners (false, entity);
                    break;
                }
            }
        }

        public class Exclude<Exc1> : EcsFilter<Inc1> where Exc1 : struct {
            protected Exclude (EcsWorld world) : base (world) {
                ExcludedTypeIndices = new[] { EcsComponentType<Exc1>.TypeIndex };
                ExcludedTypes = new[] { EcsComponentType<Exc1>.Type };
            }
        }

        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1> where Exc1 : struct where Exc2 : struct {
            protected Exclude (EcsWorld world) : base (world) {
                ExcludedTypeIndices = new[] { EcsComponentType<Exc1>.TypeIndex, EcsComponentType<Exc2>.TypeIndex };
                ExcludedTypes = new[] { EcsComponentType<Exc1>.Type, EcsComponentType<Exc2>.Type };
            }
        }
    }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
#if UNITY_2019_1_OR_NEWER
    [UnityEngine.Scripting.Preserve]
#endif
    public class EcsFilter<Inc1, Inc2> : EcsFilter where Inc1 : struct where Inc2 : struct {
        int[] _Get1;
        int[] _Get2;
        readonly bool _allow1;
        readonly bool _allow2;

        readonly EcsComponentPool<Inc1> _pool1;
        readonly EcsComponentPool<Inc2> _pool2;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref Inc1 Get1 (int idx) {
            return ref _pool1.Items[_Get1[idx]];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref Inc2 Get2 (int idx) {
            return ref _pool2.Items[_Get2[idx]];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsComponentRef<Inc1> Get1Ref (int idx) {
            return _pool1.Ref (_Get1[idx]);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsComponentRef<Inc2> Get2Ref (int idx) {
            return _pool2.Ref (_Get2[idx]);
        }

        protected EcsFilter (EcsWorld world) {
            _allow1 = !EcsComponentType<Inc1>.IsIgnoreInFilter;
            _allow2 = !EcsComponentType<Inc2>.IsIgnoreInFilter;
            _pool1 = world.GetPool<Inc1> ();
            _pool2 = world.GetPool<Inc2> ();
            _Get1 = _allow1 ? new int[EcsHelpers.FilterEntitiesSize] : null;
            _Get2 = _allow2 ? new int[EcsHelpers.FilterEntitiesSize] : null;
            IncludedTypeIndices = new[] { EcsComponentType<Inc1>.TypeIndex, EcsComponentType<Inc2>.TypeIndex };
            IncludedTypes = new[] { EcsComponentType<Inc1>.Type, EcsComponentType<Inc2>.Type };
        }

        /// <summary>
        /// Event for adding compatible entity to filter.
        /// Warning: Don't call manually!
        /// </summary>
        /// <param name="entity">Entity.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void OnAddEntity (in EcsEntity entity) {
            if (AddDelayedOp (true, entity)) { return; }
            if (Entities.Length == EntitiesCount) {
                var newSize = EntitiesCount << 1;
                Array.Resize (ref Entities, newSize);
                if (_allow1) { Array.Resize (ref _Get1, newSize); }
                if (_allow2) { Array.Resize (ref _Get2, newSize); }
            }
            // inlined and optimized World.GetComponent() call.
            ref var entityData = ref entity.Owner.GetEntityData (entity);
            var allow1 = _allow1;
            var allow2 = _allow2;
            for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2) {
                var typeIdx = entityData.Components[i];
                var itemIdx = entityData.Components[i + 1];
                if (allow1 && typeIdx == EcsComponentType<Inc1>.TypeIndex) {
                    _Get1[EntitiesCount] = itemIdx;
                    allow1 = false;
                }
                if (allow2 && typeIdx == EcsComponentType<Inc2>.TypeIndex) {
                    _Get2[EntitiesCount] = itemIdx;
                    allow2 = false;
                }
            }
            Entities[EntitiesCount++] = entity;
            ProcessListeners (true, entity);
        }

        /// <summary>
        /// Event for removing non-compatible entity to filter.
        /// Warning: Don't call manually!
        /// </summary>
        /// <param name="entity">Entity.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void OnRemoveEntity (in EcsEntity entity) {
            if (AddDelayedOp (false, entity)) { return; }
            for (int i = 0, iMax = EntitiesCount; i < iMax; i++) {
                if (Entities[i] == entity) {
                    EntitiesCount--;
                    if (i < EntitiesCount) {
                        Entities[i] = Entities[EntitiesCount];
                        if (_allow1) { _Get1[i] = _Get1[EntitiesCount]; }
                        if (_allow2) { _Get2[i] = _Get2[EntitiesCount]; }
                    }
                    ProcessListeners (false, entity);
                    break;
                }
            }
        }

        public class Exclude<Exc1> : EcsFilter<Inc1, Inc2> where Exc1 : struct {
            protected Exclude (EcsWorld world) : base (world) {
                ExcludedTypeIndices = new[] { EcsComponentType<Exc1>.TypeIndex };
                ExcludedTypes = new[] { EcsComponentType<Exc1>.Type };
            }
        }

        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1, Inc2> where Exc1 : struct where Exc2 : struct {
            protected Exclude (EcsWorld world) : base (world) {
                ExcludedTypeIndices = new[] { EcsComponentType<Exc1>.TypeIndex, EcsComponentType<Exc2>.TypeIndex };
                ExcludedTypes = new[] { EcsComponentType<Exc1>.Type, EcsComponentType<Exc2>.Type };
            }
        }
    }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
#if UNITY_2019_1_OR_NEWER
    [UnityEngine.Scripting.Preserve]
#endif
    public class EcsFilter<Inc1, Inc2, Inc3> : EcsFilter where Inc1 : struct where Inc2 : struct where Inc3 : struct {
        int[] _Get1;
        int[] _Get2;
        int[] _Get3;
        readonly bool _allow1;
        readonly bool _allow2;
        readonly bool _allow3;

        readonly EcsComponentPool<Inc1> _pool1;
        readonly EcsComponentPool<Inc2> _pool2;
        readonly EcsComponentPool<Inc3> _pool3;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref Inc1 Get1 (int idx) {
            return ref _pool1.Items[_Get1[idx]];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref Inc2 Get2 (int idx) {
            return ref _pool2.Items[_Get2[idx]];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref Inc3 Get3 (int idx) {
            return ref _pool3.Items[_Get3[idx]];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsComponentRef<Inc1> Get1Ref (int idx) {
            return _pool1.Ref (_Get1[idx]);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsComponentRef<Inc2> Get2Ref (int idx) {
            return _pool2.Ref (_Get2[idx]);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsComponentRef<Inc3> Get3Ref (int idx) {
            return _pool3.Ref (_Get3[idx]);
        }

        protected EcsFilter (EcsWorld world) {
            _allow1 = !EcsComponentType<Inc1>.IsIgnoreInFilter;
            _allow2 = !EcsComponentType<Inc2>.IsIgnoreInFilter;
            _allow3 = !EcsComponentType<Inc3>.IsIgnoreInFilter;
            _pool1 = world.GetPool<Inc1> ();
            _pool2 = world.GetPool<Inc2> ();
            _pool3 = world.GetPool<Inc3> ();
            _Get1 = _allow1 ? new int[EcsHelpers.FilterEntitiesSize] : null;
            _Get2 = _allow2 ? new int[EcsHelpers.FilterEntitiesSize] : null;
            _Get3 = _allow3 ? new int[EcsHelpers.FilterEntitiesSize] : null;
            IncludedTypeIndices = new[] { EcsComponentType<Inc1>.TypeIndex, EcsComponentType<Inc2>.TypeIndex, EcsComponentType<Inc3>.TypeIndex };
            IncludedTypes = new[] { EcsComponentType<Inc1>.Type, EcsComponentType<Inc2>.Type, EcsComponentType<Inc3>.Type };
        }

        /// <summary>
        /// Event for adding compatible entity to filter.
        /// Warning: Don't call manually!
        /// </summary>
        /// <param name="entity">Entity.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void OnAddEntity (in EcsEntity entity) {
            if (AddDelayedOp (true, entity)) { return; }
            if (Entities.Length == EntitiesCount) {
                var newSize = EntitiesCount << 1;
                Array.Resize (ref Entities, newSize);
                if (_allow1) { Array.Resize (ref _Get1, newSize); }
                if (_allow2) { Array.Resize (ref _Get2, newSize); }
                if (_allow3) { Array.Resize (ref _Get3, newSize); }
            }
            // inlined and optimized World.GetComponent() call.
            ref var entityData = ref entity.Owner.GetEntityData (entity);
            var allow1 = _allow1;
            var allow2 = _allow2;
            var allow3 = _allow3;
            for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2) {
                var typeIdx = entityData.Components[i];
                var itemIdx = entityData.Components[i + 1];
                if (allow1 && typeIdx == EcsComponentType<Inc1>.TypeIndex) {
                    _Get1[EntitiesCount] = itemIdx;
                    allow1 = false;
                }
                if (allow2 && typeIdx == EcsComponentType<Inc2>.TypeIndex) {
                    _Get2[EntitiesCount] = itemIdx;
                    allow2 = false;
                }
                if (allow3 && typeIdx == EcsComponentType<Inc3>.TypeIndex) {
                    _Get3[EntitiesCount] = itemIdx;
                    allow3 = false;
                }
            }
            Entities[EntitiesCount++] = entity;
            ProcessListeners (true, entity);
        }

        /// <summary>
        /// Event for removing non-compatible entity to filter.
        /// Warning: Don't call manually!
        /// </summary>
        /// <param name="entity">Entity.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void OnRemoveEntity (in EcsEntity entity) {
            if (AddDelayedOp (false, entity)) { return; }
            for (int i = 0, iMax = EntitiesCount; i < iMax; i++) {
                if (Entities[i] == entity) {
                    EntitiesCount--;
                    if (i < EntitiesCount) {
                        Entities[i] = Entities[EntitiesCount];
                        if (_allow1) { _Get1[i] = _Get1[EntitiesCount]; }
                        if (_allow2) { _Get2[i] = _Get2[EntitiesCount]; }
                        if (_allow3) { _Get3[i] = _Get3[EntitiesCount]; }
                    }
                    ProcessListeners (false, entity);
                    break;
                }
            }
        }

        public class Exclude<Exc1> : EcsFilter<Inc1, Inc2, Inc3> where Exc1 : struct {
            protected Exclude (EcsWorld world) : base (world) {
                ExcludedTypeIndices = new[] { EcsComponentType<Exc1>.TypeIndex };
                ExcludedTypes = new[] { EcsComponentType<Exc1>.Type };
            }
        }

        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1, Inc2, Inc3> where Exc1 : struct where Exc2 : struct {
            protected Exclude (EcsWorld world) : base (world) {
                ExcludedTypeIndices = new[] { EcsComponentType<Exc1>.TypeIndex, EcsComponentType<Exc2>.TypeIndex };
                ExcludedTypes = new[] { EcsComponentType<Exc1>.Type, EcsComponentType<Exc2>.Type };
            }
        }
    }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
#if UNITY_2019_1_OR_NEWER
    [UnityEngine.Scripting.Preserve]
#endif
    public class EcsFilter<Inc1, Inc2, Inc3, Inc4> : EcsFilter where Inc1 : struct where Inc2 : struct where Inc3 : struct where Inc4 : struct {
        int[] _Get1;
        int[] _Get2;
        int[] _Get3;
        int[] _Get4;
        readonly bool _allow1;
        readonly bool _allow2;
        readonly bool _allow3;
        readonly bool _allow4;

        readonly EcsComponentPool<Inc1> _pool1;
        readonly EcsComponentPool<Inc2> _pool2;
        readonly EcsComponentPool<Inc3> _pool3;
        readonly EcsComponentPool<Inc4> _pool4;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref Inc1 Get1 (int idx) {
            return ref _pool1.Items[_Get1[idx]];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref Inc2 Get2 (int idx) {
            return ref _pool2.Items[_Get2[idx]];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref Inc3 Get3 (int idx) {
            return ref _pool3.Items[_Get3[idx]];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref Inc4 Get4 (int idx) {
            return ref _pool4.Items[_Get4[idx]];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsComponentRef<Inc1> Get1Ref (int idx) {
            return _pool1.Ref (_Get1[idx]);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsComponentRef<Inc2> Get2Ref (int idx) {
            return _pool2.Ref (_Get2[idx]);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsComponentRef<Inc3> Get3Ref (int idx) {
            return _pool3.Ref (_Get3[idx]);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsComponentRef<Inc4> Get4Ref (int idx) {
            return _pool4.Ref (_Get4[idx]);
        }

        protected EcsFilter (EcsWorld world) {
            _allow1 = !EcsComponentType<Inc1>.IsIgnoreInFilter;
            _allow2 = !EcsComponentType<Inc2>.IsIgnoreInFilter;
            _allow3 = !EcsComponentType<Inc3>.IsIgnoreInFilter;
            _allow4 = !EcsComponentType<Inc4>.IsIgnoreInFilter;
            _pool1 = world.GetPool<Inc1> ();
            _pool2 = world.GetPool<Inc2> ();
            _pool3 = world.GetPool<Inc3> ();
            _pool4 = world.GetPool<Inc4> ();
            _Get1 = _allow1 ? new int[EcsHelpers.FilterEntitiesSize] : null;
            _Get2 = _allow2 ? new int[EcsHelpers.FilterEntitiesSize] : null;
            _Get3 = _allow3 ? new int[EcsHelpers.FilterEntitiesSize] : null;
            _Get4 = _allow4 ? new int[EcsHelpers.FilterEntitiesSize] : null;
            IncludedTypeIndices = new[] { EcsComponentType<Inc1>.TypeIndex, EcsComponentType<Inc2>.TypeIndex, EcsComponentType<Inc3>.TypeIndex, EcsComponentType<Inc4>.TypeIndex };
            IncludedTypes = new[] { EcsComponentType<Inc1>.Type, EcsComponentType<Inc2>.Type, EcsComponentType<Inc3>.Type, EcsComponentType<Inc4>.Type };
        }

        /// <summary>
        /// Event for adding compatible entity to filter.
        /// Warning: Don't call manually!
        /// </summary>
        /// <param name="entity">Entity.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void OnAddEntity (in EcsEntity entity) {
            if (AddDelayedOp (true, entity)) { return; }
            if (Entities.Length == EntitiesCount) {
                var newSize = EntitiesCount << 1;
                Array.Resize (ref Entities, newSize);
                if (_allow1) { Array.Resize (ref _Get1, newSize); }
                if (_allow2) { Array.Resize (ref _Get2, newSize); }
                if (_allow3) { Array.Resize (ref _Get3, newSize); }
                if (_allow4) { Array.Resize (ref _Get4, newSize); }
            }
            // inlined and optimized World.GetComponent() call.
            ref var entityData = ref entity.Owner.GetEntityData (entity);
            var allow1 = _allow1;
            var allow2 = _allow2;
            var allow3 = _allow3;
            var allow4 = _allow4;
            for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2) {
                var typeIdx = entityData.Components[i];
                var itemIdx = entityData.Components[i + 1];
                if (allow1 && typeIdx == EcsComponentType<Inc1>.TypeIndex) {
                    _Get1[EntitiesCount] = itemIdx;
                    allow1 = false;
                }
                if (allow2 && typeIdx == EcsComponentType<Inc2>.TypeIndex) {
                    _Get2[EntitiesCount] = itemIdx;
                    allow2 = false;
                }
                if (allow3 && typeIdx == EcsComponentType<Inc3>.TypeIndex) {
                    _Get3[EntitiesCount] = itemIdx;
                    allow3 = false;
                }
                if (allow4 && typeIdx == EcsComponentType<Inc4>.TypeIndex) {
                    _Get4[EntitiesCount] = itemIdx;
                    allow4 = false;
                }
            }
            Entities[EntitiesCount++] = entity;
            ProcessListeners (true, entity);
        }

        /// <summary>
        /// Event for removing non-compatible entity to filter.
        /// Warning: Don't call manually!
        /// </summary>
        /// <param name="entity">Entity.</param>
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public override void OnRemoveEntity (in EcsEntity entity) {
            if (AddDelayedOp (false, entity)) { return; }
            for (int i = 0, iMax = EntitiesCount; i < iMax; i++) {
                if (Entities[i] == entity) {
                    EntitiesCount--;
                    if (i < EntitiesCount) {
                        Entities[i] = Entities[EntitiesCount];
                        if (_allow1) { _Get1[i] = _Get1[EntitiesCount]; }
                        if (_allow2) { _Get2[i] = _Get2[EntitiesCount]; }
                        if (_allow3) { _Get3[i] = _Get3[EntitiesCount]; }
                        if (_allow4) { _Get4[i] = _Get4[EntitiesCount]; }
                    }
                    ProcessListeners (false, entity);
                    break;
                }
            }
        }

        public class Exclude<Exc1> : EcsFilter<Inc1, Inc2, Inc3, Inc4> where Exc1 : struct {
            protected Exclude (EcsWorld world) : base (world) {
                ExcludedTypeIndices = new[] { EcsComponentType<Exc1>.TypeIndex };
                ExcludedTypes = new[] { EcsComponentType<Exc1>.Type };
            }
        }

        public class Exclude<Exc1, Exc2> : EcsFilter<Inc1, Inc2, Inc3, Inc4> where Exc1 : struct where Exc2 : struct {
            protected Exclude (EcsWorld world) : base (world) {
                ExcludedTypeIndices = new[] { EcsComponentType<Exc1>.TypeIndex, EcsComponentType<Exc2>.TypeIndex };
                ExcludedTypes = new[] { EcsComponentType<Exc1>.Type, EcsComponentType<Exc2>.Type };
            }
        }
    }
}