// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class ImageItemLoadingAnimator : MonoBehaviour
    {
        [SerializeField] private float _animationSpeed;
        
        private void Update()
        {
            transform.Rotate(new Vector3(0, 0, 1), _animationSpeed * Time.deltaTime);
        }
    }
}