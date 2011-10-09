using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace RayTraceProject
{
    class Camera
    {
        private Matrix view, projection, world;
        private float fieldOfView;
        private float aspectRatio;
        private float nearClippingPlane;
        private float farClippingPlane;
        private Vector3 position;
        private Vector3 target;
        private Vector3 up;
        private bool viewIsDirty;
        private bool projectionIsDirty;
        private bool boundingFrustumIsDirty;
        private BoundingFrustum boundingFrustum;
        public event EventHandler<EventArgs> ViewChanged;

        public Camera(Vector3 position, Vector3 target, Vector3 up, float fieldOfView, float aspectRatio, float nearClippingPlane, float farClippingPlane)
        {
            this.fieldOfView = fieldOfView;
            this.aspectRatio = aspectRatio;
            this.position = position;
            this.target = target;
            this.up = up;
            this.nearClippingPlane = nearClippingPlane;
            this.farClippingPlane = farClippingPlane;

            this.viewIsDirty = true;
            this.projectionIsDirty = true;
            this.boundingFrustumIsDirty = true;
        }

        private void CreateView()
        {
            Matrix.CreateLookAt(ref this.position, ref this.target, ref this.up, out this.view);
            this.viewIsDirty = false;
            this.boundingFrustumIsDirty = true;
            if (this.ViewChanged != null)
                this.ViewChanged(this, EventArgs.Empty);
        }

        private void CreateProjection()
        {
            Matrix.CreatePerspectiveFieldOfView(this.fieldOfView, this.aspectRatio, this.nearClippingPlane, this.farClippingPlane, out this.projection);
            this.projectionIsDirty = false;
            this.boundingFrustumIsDirty = true;
        }

        private void CreateBoundingFrustum()
        {
            this.boundingFrustum = new BoundingFrustum(this.View * this.Projection);
            this.boundingFrustumIsDirty = false;
        }

        public Vector3 Position
        {
            get { return this.position; }
            set
            {
                if (this.position != value)
                {
                    this.position = value;
                    this.viewIsDirty = true;
                }
            }
        }

        public Vector3 Target
        {
            get { return this.target; }
            set
            {
                if (this.target != value)
                {
                    this.target = value;
                    this.viewIsDirty = true;
                }
            }
        }

        public Vector3 Up
        {
            get { return this.up; }
            set
            {
                if (this.up != value)
                {
                    this.up = value;
                    this.viewIsDirty = true;
                }
            }
        }

        public float NearClippingPlane
        {
            get { return this.nearClippingPlane; }
            set
            {
                if (this.nearClippingPlane != value)
                {
                    this.nearClippingPlane = value;
                    this.projectionIsDirty = true;
                }
            }
        }

        public float FarClippingPlane
        {
            get { return this.farClippingPlane; }
            set
            {
                if (this.farClippingPlane != value)
                {
                    this.farClippingPlane = value;
                    this.projectionIsDirty = true;
                }
            }
        }

        public float AspectRatio
        {
            get { return this.aspectRatio; }
            set
            {
                if (this.aspectRatio != value)
                {
                    this.aspectRatio = value;
                    this.projectionIsDirty = true;
                }
            }
        }

        public float FieldOfView
        {
            get { return this.fieldOfView; }
            set
            {
                if (this.fieldOfView != value)
                {
                    this.fieldOfView = value;
                    this.projectionIsDirty = true;
                }
            }
        }

        public Matrix View
        {
            get
            {
                if (this.viewIsDirty)
                    this.CreateView();
                return this.view;
            }
        }

        public Matrix Projection
        {
            get
            {
                if (this.projectionIsDirty)
                    this.CreateProjection();

                return this.projection;
            }
        }

        public Matrix World
        {
            get
            {
                return Matrix.CreateTranslation(this.position);
            }
        }

        public BoundingFrustum BoundingFrustum
        {
            get
            {
                if (this.boundingFrustumIsDirty)
                    this.CreateBoundingFrustum();

                return this.boundingFrustum;
            }
        }
    }
}
