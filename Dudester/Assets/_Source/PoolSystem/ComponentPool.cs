using System.Collections.Generic;
using UnityEngine;

namespace PoolSystem
{
    // Fixed capacity: no Instantiate/Destroy or collection resizing during combat.
    public sealed class ComponentPool<T> where T : Component
    {
        private readonly Stack<T> available;
        private readonly HashSet<T> rented;
        public int Capacity { get; }
        public int ActiveCount => rented.Count;

        public ComponentPool(T prefab, int capacity, Transform parent)
        {
            Capacity = Mathf.Max(1, capacity);
            available = new Stack<T>(Capacity);
            rented = new HashSet<T>(Capacity);
            for (int i = 0; i < Capacity; i++)
            {
                T item = Object.Instantiate(prefab, parent);
                item.gameObject.SetActive(false);
                available.Push(item);
            }
        }

        public T Rent()
        {
            if (available.Count == 0) return null;
            T item = available.Pop();
            rented.Add(item);
            item.gameObject.SetActive(true);
            return item;
        }

        public void Return(T item)
        {
            if (item == null || !rented.Remove(item)) return;
            item.gameObject.SetActive(false);
            available.Push(item);
        }
    }
}
