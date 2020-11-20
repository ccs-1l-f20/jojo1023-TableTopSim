using GameLib.GameSerialization;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using System.Diagnostics;

namespace GameLib.Sprites
{
    public class Transform
    {
        Vector2 position;

        [GameSerializableData(1)]
        public Vector2 Position { get => position; set { 
                position = value; 
                NotifyPropertyChanged(1); } }
        public float X { get { return Position.X; } set { Position = new Vector2(value, Position.Y); } }
        public float Y { get { return Position.Y; } set { Position = new Vector2(Position.X, value); } }

        Vector2 scale;
        [GameSerializableData(2)]
        public Vector2 Scale { get => scale; set { scale = value; NotifyPropertyChanged(2); } }


        float rotation;
        [GameSerializableData(3)]
        public float Rotation { get => rotation; set { rotation = value; NotifyPropertyChanged(3); } }

        int? parent = null;

        [GameSerializableData(4)]
        public int? Parent { get => parent; set { parent = value; NotifyPropertyChanged(4); } }

        bool hasId = false;

        public event Action<Transform, ushort> OnPropertyChanged;
        SpriteRefrenceManager refManager = null;
        Sprite sprite;

        static Transform()
        {
            GameSerialize.AddType<Transform>(GameSerialize.GenericSerializeFunc, GameSerialize.GenericDeserializeFunc, true);
        }
        public Transform()
        {
            position = Vector2.Zero;
            scale = Vector2.One;
            rotation = 0;
            parent = null;
            sprite = null;
        }
        public Transform(Sprite sprite)
        {
            position = Vector2.Zero;
            scale = Vector2.One;
            rotation = 0;
            parent = null;
            this.sprite = sprite;
        }
        public void SetRefManager(SpriteRefrenceManager refManager, Sprite sprite)
        {
            this.refManager = refManager;
            this.sprite = sprite;
            hasId = true;
        }
        public Transform(Vector2 position, Vector2 scale, float rotation, Sprite sprite)
        {
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
            parent = null;
            this.sprite = sprite;
        }

        protected void NotifyPropertyChanged(ushort propertyDataId)
        {
            OnPropertyChanged?.Invoke(this, propertyDataId);
        }


        public Matrix<float> GetMatrix()
        {
            Matrix<float> matrix = CreateMatrix.Dense<float>(3, 3);
            matrix[2, 2] = 1;

            matrix[0, 2] = position.X;
            matrix[1, 2] = position.Y;

            double radians = Extensions.DegreesToRadians(-rotation);
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);
            matrix[0, 0] = cos * scale.X;
            matrix[1, 0] = sin * scale.X;

            matrix[0, 1] = -sin * scale.Y;
            matrix[1, 1] = cos * scale.Y;

            return matrix;
        }

        public Matrix<float> GetGlobalMatrix(Dictionary<int, Matrix<float>> spriteMatries = null)
        {
            int spriteId = refManager.GetAddress(sprite);
            if(hasId && spriteMatries != null && spriteMatries.ContainsKey(spriteId))
            {
                return spriteMatries[spriteId];
            }
            Matrix<float> matrix;
            if (parent == null)
            {
                matrix = GetMatrix();
            }
            else
            {
                if (refManager == null) { throw new NullReferenceException(); }
                //Debug.WriteLine($"Sprite Id: {spriteId}");
                Sprite parentSprite = refManager.GetSprite(parent.Value);
                matrix = parentSprite.Transform.GetGlobalMatrix() * GetMatrix();
            }
            if(hasId && spriteMatries != null)
            {
                spriteMatries.Add(spriteId, matrix);
            }
            return matrix;
        }
        static Vector2 RotatePoint(Vector2 rotPoint, float rotation)
        {
            double currentRotation = Math.Atan2(rotPoint.Y, rotPoint.X);
            double distance = rotPoint.Length();
            double rotRadians = Extensions.DegreesToRadians(rotation);
            rotPoint.X = (float)(Math.Cos(currentRotation - rotRadians) * distance);
            rotPoint.Y = (float)(Math.Sin(currentRotation - rotRadians) * distance);

            return rotPoint;
        }
        public Vector2 GetGlobalPosition()
        {
            if (parent == null) { return Position; }
            if (refManager == null) { throw new NullReferenceException(); }
            Sprite parentSprite = refManager.GetSprite(parent.Value);
            Vector2 parentPos = parentSprite.Transform.GetGlobalPosition();

            Vector2 relPositon = Position * parentSprite.Transform.Scale;
            relPositon = RotatePoint(relPositon, parentSprite.Transform.Rotation);
            return relPositon + parentPos;
        }
        public float GetGlobalRotation()
        {
            if (parent == null) { return Rotation; }
            if (refManager == null) { throw new NullReferenceException(); }
            Sprite parentSprite = refManager.GetSprite(parent.Value);
            float parentRot = parentSprite.Transform.GetGlobalRotation();
            return parentRot + Rotation;
        }

        public static Vector2 TransformPoint(Matrix<float> matrix, Vector2 point)
        {
            Matrix<float> pointMatrix = CreateMatrix.Dense<float>(3, 1);
            pointMatrix[0, 0] = point.X;
            pointMatrix[1, 0] = point.Y;
            pointMatrix[2, 0] = 1;
            pointMatrix = matrix * pointMatrix;
            return new Vector2(pointMatrix[0, 0], pointMatrix[1, 0]);
        }

        public static Matrix<float> InverseTransformMatrix(Matrix<float> matrix)
        {
            float determinateThing = (matrix[0, 1] * matrix[1, 0]) - (matrix[0, 0] * matrix[1, 1]);
            Matrix<float> inverse = CreateMatrix.Dense<float>(3, 3);
            if (determinateThing == 0)
            {
                return inverse;
            }
            matrix.CopyTo(inverse);
            inverse[0, 0] = -matrix[1, 1];
            inverse[1, 1] = -matrix[0, 0];
            inverse[0, 2] = (matrix[1, 1] * matrix[0, 2]) - (matrix[0, 1] * matrix[1, 2]);
            inverse[1, 2] = (matrix[0, 0] * matrix[1, 2]) - (matrix[1, 0] * matrix[0, 2]);
            inverse = inverse / determinateThing;
            inverse[2,2] = 1;
            return inverse;
        }
    }
}
