using System;
using UnityEngine;

namespace RecyclableSR
{
    [Serializable]
    public class IDataSourceContainer
    {
        public IDataSource DataSource
        {
            get => _dataSource as IDataSource;
            set => _dataSource = value as UnityEngine.Object;
        }

        [SerializeField] private UnityEngine.Object _dataSource;
    }
}