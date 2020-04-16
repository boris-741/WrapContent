using System;
using Leopotam.EcsNew;

namespace WrapContent
{
    public static class EcsExtensions
    {
        public static EcsFilter<T1> GetFilter<T1>(this EcsWorld world) 
                                where T1: struct
        {
            return (EcsFilter<T1>)world.GetFilter(typeof(EcsFilter<T1>)); 
        }
        public static EcsFilter<T1, T2> GetFilter<T1, T2>(this EcsWorld world) 
                                where T1: struct where T2: struct
        {
            return (EcsFilter<T1, T2>)world.GetFilter(typeof(EcsFilter<T1, T2>)); 
        }
        public static EcsFilter<T1, T2, T3> GetFilter<T1, T2, T3>(this EcsWorld world) 
                                where T1: struct where T2: struct where T3: struct
        {
            return (EcsFilter<T1, T2, T3>)world.GetFilter(typeof(EcsFilter<T1, T2, T3>)); 
        }
        /// <summary>
        /// simple clear filter
        /// </summary>
        /// <param name="filter"></param>
        public static void ClearSimple(this EcsFilter filter)
        {
            foreach (int i in filter) 
            {
                ref EcsEntity entity = ref filter.GetEntity( i );
                entity.Destroy();
            }
        }
    }
}