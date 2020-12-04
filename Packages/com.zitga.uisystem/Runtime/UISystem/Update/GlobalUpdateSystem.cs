using System;
using System.Collections.Generic;

namespace Loxodon.Framework.Views
{
    public class GlobalUpdateSystem : IUpdateSystem
    {
        private readonly object _lock = new object();
        
        private readonly List<IUpdateSystem> updates;
        
        /// <summary>
        /// Current number update function
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return updates.Count;
                }
            }
        }

        public GlobalUpdateSystem()
        {
            updates = new List<IUpdateSystem>();
        }
        
        public void OnUpdate(float deltaTime)
        {
            lock (_lock)
            {
                foreach (IUpdateSystem update in updates)
                {
                    update?.OnUpdate(deltaTime);
                }
            }
        }

        /// <summary>
        /// Add update function from someone object what need to call
        /// </summary>
        /// <param name="update"></param>
        /// <exception cref="Exception"></exception>
        public void Add(IUpdateSystem update)
        {
            lock (_lock)
            {
                if (updates.Contains(update))
                {
                    throw new Exception($"Object is exist in updates: {nameof(update)}");
                }
                updates.Add(update);
            }
        }
        
        /// <summary>
        /// remove update from a object when it does not use
        /// </summary>
        /// <param name="update"></param>
        /// <exception cref="Exception"></exception>
        public void Remove(IUpdateSystem update)
        {
            lock (_lock)
            {
                if (updates.Contains(update))
                {
                    updates.Remove(update);
                }
                else
                {
                    throw new Exception($"Object is not exist in updates: {nameof(update)}");
                }
            }
        }
    }
}

