using System;
using UnityEngine;
using System.Reflection;
using Rhinox.Lightspeed;

namespace Rhinox.Utilities
{
	public static class GizmosExt
	{
		public static void DrawPoint(Vector3 position, Color color, float scale = 1.0f)
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = color;
			Gizmos.DrawRay(position + (Vector3.up * (scale * 0.5f)), -Vector3.up * scale);
			Gizmos.DrawRay(position + (Vector3.right * (scale * 0.5f)), -Vector3.right * scale);
			Gizmos.DrawRay(position + (Vector3.forward * (scale * 0.5f)), -Vector3.forward * scale);

			Gizmos.color = oldColor;
		}

		public static void DrawLine(Vector3 start, Vector3 end, Color color)
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = color;

			Gizmos.DrawLine(start, end);

			Gizmos.color = oldColor;
		}

		public static void DrawPoint(Vector3 position, float scale = 1.0f)
		{
			DrawPoint(position, Color.white, scale);
		}

		public static void DrawBounds(Bounds bounds, Color color)
		{
			Vector3 center = bounds.center;

			float x = bounds.extents.x;
			float y = bounds.extents.y;
			float z = bounds.extents.z;

			Vector3 ruf = center + new Vector3(x, y, z);
			Vector3 rub = center + new Vector3(x, y, -z);
			Vector3 luf = center + new Vector3(-x, y, z);
			Vector3 lub = center + new Vector3(-x, y, -z);

			Vector3 rdf = center + new Vector3(x, -y, z);
			Vector3 rdb = center + new Vector3(x, -y, -z);
			Vector3 lfd = center + new Vector3(-x, -y, z);
			Vector3 lbd = center + new Vector3(-x, -y, -z);

			Color oldColor = Gizmos.color;
			Gizmos.color = color;

			Gizmos.DrawLine(ruf, luf);
			Gizmos.DrawLine(ruf, rub);
			Gizmos.DrawLine(luf, lub);
			Gizmos.DrawLine(rub, lub);

			Gizmos.DrawLine(ruf, rdf);
			Gizmos.DrawLine(rub, rdb);
			Gizmos.DrawLine(luf, lfd);
			Gizmos.DrawLine(lub, lbd);

			Gizmos.DrawLine(rdf, lfd);
			Gizmos.DrawLine(rdf, rdb);
			Gizmos.DrawLine(lfd, lbd);
			Gizmos.DrawLine(lbd, rdb);

			Gizmos.color = oldColor;
		}

		public static void DrawBounds(Bounds bounds)
		{
			DrawBounds(bounds, Color.white);
		}

		public static void DrawLocalCube(Transform transform, Vector3 size, Color color,
			Vector3 center = default(Vector3))
		{
			Color oldColor = Gizmos.color;
			Gizmos.color = color;

			Vector3 lbb = transform.TransformPoint(center + ((-size) * 0.5f));
			Vector3 rbb = transform.TransformPoint(center + (new Vector3(size.x, -size.y, -size.z) * 0.5f));

			Vector3 lbf = transform.TransformPoint(center + (new Vector3(size.x, -size.y, size.z) * 0.5f));
			Vector3 rbf = transform.TransformPoint(center + (new Vector3(-size.x, -size.y, size.z) * 0.5f));

			Vector3 lub = transform.TransformPoint(center + (new Vector3(-size.x, size.y, -size.z) * 0.5f));
			Vector3 rub = transform.TransformPoint(center + (new Vector3(size.x, size.y, -size.z) * 0.5f));

			Vector3 luf = transform.TransformPoint(center + ((size) * 0.5f));
			Vector3 ruf = transform.TransformPoint(center + (new Vector3(-size.x, size.y, size.z) * 0.5f));

			Gizmos.DrawLine(lbb, rbb);
			Gizmos.DrawLine(rbb, lbf);
			Gizmos.DrawLine(lbf, rbf);
			Gizmos.DrawLine(rbf, lbb);

			Gizmos.DrawLine(lub, rub);
			Gizmos.DrawLine(rub, luf);
			Gizmos.DrawLine(luf, ruf);
			Gizmos.DrawLine(ruf, lub);

			Gizmos.DrawLine(lbb, lub);
			Gizmos.DrawLine(rbb, rub);
			Gizmos.DrawLine(lbf, luf);
			Gizmos.DrawLine(rbf, ruf);

			Gizmos.color = oldColor;
		}

		public static void DrawLocalCube(Transform transform, Vector3 size, Vector3 center = default(Vector3))
		{
			DrawLocalCube(transform, size, Color.white, center);
		}

		public static void DrawLocalCube(Matrix4x4 space, Vector3 size, Color color, Vector3 center = default(Vector3))
		{
			Color oldColor = Gizmos.color;
			Gizmos.color = color;

			Vector3 lbb = space.MultiplyPoint3x4(center + ((-size) * 0.5f));
			Vector3 rbb = space.MultiplyPoint3x4(center + (new Vector3(size.x, -size.y, -size.z) * 0.5f));

			Vector3 lbf = space.MultiplyPoint3x4(center + (new Vector3(size.x, -size.y, size.z) * 0.5f));
			Vector3 rbf = space.MultiplyPoint3x4(center + (new Vector3(-size.x, -size.y, size.z) * 0.5f));

			Vector3 lub = space.MultiplyPoint3x4(center + (new Vector3(-size.x, size.y, -size.z) * 0.5f));
			Vector3 rub = space.MultiplyPoint3x4(center + (new Vector3(size.x, size.y, -size.z) * 0.5f));

			Vector3 luf = space.MultiplyPoint3x4(center + ((size) * 0.5f));
			Vector3 ruf = space.MultiplyPoint3x4(center + (new Vector3(-size.x, size.y, size.z) * 0.5f));

			Gizmos.DrawLine(lbb, rbb);
			Gizmos.DrawLine(rbb, lbf);
			Gizmos.DrawLine(lbf, rbf);
			Gizmos.DrawLine(rbf, lbb);

			Gizmos.DrawLine(lub, rub);
			Gizmos.DrawLine(rub, luf);
			Gizmos.DrawLine(luf, ruf);
			Gizmos.DrawLine(ruf, lub);

			Gizmos.DrawLine(lbb, lub);
			Gizmos.DrawLine(rbb, rub);
			Gizmos.DrawLine(lbf, luf);
			Gizmos.DrawLine(rbf, ruf);

			Gizmos.color = oldColor;
		}

		public static void DrawLocalCube(Matrix4x4 space, Vector3 size, Vector3 center = default(Vector3))
		{
			DrawLocalCube(space, size, Color.white, center);
		}

		public static void DrawCircle(Vector3 position, Vector3 up, Color color, float radius = 1.0f)
		{
			up = ((up == Vector3.zero) ? Vector3.up : up).normalized * radius;
			Vector3 _forward = Vector3.Slerp(up, -up, 0.5f);
			Vector3 _right = Vector3.Cross(up, _forward).normalized * radius;

			Matrix4x4 matrix = new Matrix4x4();

			matrix[0] = _right.x;
			matrix[1] = _right.y;
			matrix[2] = _right.z;

			matrix[4] = up.x;
			matrix[5] = up.y;
			matrix[6] = up.z;

			matrix[8] = _forward.x;
			matrix[9] = _forward.y;
			matrix[10] = _forward.z;

			Vector3 _lastPoint = position + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)));
			Vector3 _nextPoint = Vector3.zero;

			Color oldColor = Gizmos.color;
			Gizmos.color = (color == default(Color)) ? Color.white : color;

			for (var i = 0; i < 91; i++)
			{
				_nextPoint.x = Mathf.Cos((i * 4) * Mathf.Deg2Rad);
				_nextPoint.z = Mathf.Sin((i * 4) * Mathf.Deg2Rad);
				_nextPoint.y = 0;

				_nextPoint = position + matrix.MultiplyPoint3x4(_nextPoint);

				Gizmos.DrawLine(_lastPoint, _nextPoint);
				_lastPoint = _nextPoint;
			}

			Gizmos.color = oldColor;
		}

		public static void DrawCircle(Vector3 position, Color color, float radius = 1.0f)
		{
			DrawCircle(position, Vector3.up, color, radius);
		}

		public static void DrawCircle(Vector3 position, Vector3 up, float radius = 1.0f)
		{
			DrawCircle(position, position, Gizmos.color, radius);
		}

		public static void DrawCircle(Vector3 position, float radius = 1.0f)
		{
			DrawCircle(position, Vector3.up, Gizmos.color, radius);
		}

		public static void DrawCylinder(Vector3 start, Vector3 end, Color color, float radius = 1.0f)
		{
			Vector3 up = (end - start).normalized * radius;
			Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
			Vector3 right = Vector3.Cross(up, forward).normalized * radius;

			//Radial circles
			GizmosExt.DrawCircle(start, up, color, radius);
			GizmosExt.DrawCircle(end, -up, color, radius);
			GizmosExt.DrawCircle((start + end) * 0.5f, up, color, radius);

			Color oldColor = Gizmos.color;
			Gizmos.color = color;

			//Side lines
			Gizmos.DrawLine(start + right, end + right);
			Gizmos.DrawLine(start - right, end - right);

			Gizmos.DrawLine(start + forward, end + forward);
			Gizmos.DrawLine(start - forward, end - forward);

			//Start endcap
			Gizmos.DrawLine(start - right, start + right);
			Gizmos.DrawLine(start - forward, start + forward);

			//End endcap
			Gizmos.DrawLine(end - right, end + right);
			Gizmos.DrawLine(end - forward, end + forward);

			Gizmos.color = oldColor;
		}

		public static void DrawCylinder(Vector3 start, Vector3 end, float radius = 1.0f)
		{
			DrawCylinder(start, end, Gizmos.color, radius);
		}

		public static void DrawSquareTorus(Vector3 start, Vector3 end, Color color, float radius = 1.0f, float innerRadius = .75f)
		{
			Vector3 up = (end - start).normalized * radius;
			Vector3 upInner = (end - start).normalized * innerRadius;
			Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
			Vector3 forwardInner = Vector3.Slerp(upInner, -upInner, 0.5f);
			Vector3 right = Vector3.Cross(up, forward).normalized * radius;
			Vector3 rightInner = Vector3.Cross(up, forward).normalized * innerRadius;

			//Radial circles
			GizmosExt.DrawCircle(start, up, color, radius);
			GizmosExt.DrawCircle(start, up, color, innerRadius);
			GizmosExt.DrawCircle(end, -up, color, radius);
			GizmosExt.DrawCircle(end, -up, color, innerRadius);

			Color oldColor = Gizmos.color;
			Gizmos.color = color;

			//Side lines
			Gizmos.DrawLine(start + right, end + right);
			Gizmos.DrawLine(start - right, end - right);

			Gizmos.DrawLine(start + forward, end + forward);
			Gizmos.DrawLine(start - forward, end - forward);

			Gizmos.DrawLine(start + rightInner, end + rightInner);
			Gizmos.DrawLine(start - rightInner, end - rightInner);

			Gizmos.DrawLine(start + forwardInner, end + forwardInner);
			Gizmos.DrawLine(start - forwardInner, end - forwardInner);

			//Start endcap
			Gizmos.DrawLine(start - right, start - rightInner);
			Gizmos.DrawLine(start + rightInner, start + right);
			Gizmos.DrawLine(start - forward, start - forwardInner);
			Gizmos.DrawLine(start + forwardInner, start + forward);

			//End endcap
			Gizmos.DrawLine(end - right, end - rightInner);
			Gizmos.DrawLine(end + rightInner, end + right);
			Gizmos.DrawLine(end - forward, end - forwardInner);
			Gizmos.DrawLine(end + forwardInner, end + forward);

			Gizmos.color = oldColor;
		}

		public static void DrawSquareTorus(Vector3 start, Vector3 end, float radius = 1.0f, float innerRadius = .75f)
		{
			DrawSquareTorus(start, end, Gizmos.color, radius, innerRadius);
		}

		public static void DrawCone(Vector3 position, Vector3 direction, Color color, float angle = 45)
		{
			float length = direction.magnitude;

			Vector3 _forward = direction;
			Vector3 _up = Vector3.Slerp(_forward, -_forward, 0.5f);
			Vector3 _right = Vector3.Cross(_forward, _up).normalized * length;

			direction = direction.normalized;

			Vector3 slerpedVector = Vector3.Slerp(_forward, _up, angle / 90.0f);

			float dist;
			var farPlane = new Plane(-direction, position + _forward);
			var distRay = new Ray(position, slerpedVector);

			farPlane.Raycast(distRay, out dist);

			Color oldColor = Gizmos.color;
			Gizmos.color = color;

			Gizmos.DrawRay(position, slerpedVector.normalized * dist);
			Gizmos.DrawRay(position, Vector3.Slerp(_forward, -_up, angle / 90.0f).normalized * dist);
			Gizmos.DrawRay(position, Vector3.Slerp(_forward, _right, angle / 90.0f).normalized * dist);
			Gizmos.DrawRay(position, Vector3.Slerp(_forward, -_right, angle / 90.0f).normalized * dist);

			GizmosExt.DrawCircle(position + _forward, direction, color,
				(_forward - (slerpedVector.normalized * dist)).magnitude);
			GizmosExt.DrawCircle(position + (_forward * 0.5f), direction, color,
				((_forward * 0.5f) - (slerpedVector.normalized * (dist * 0.5f))).magnitude);

			Gizmos.color = oldColor;
		}

		public static void DrawCone(Vector3 position, Vector3 direction, float angle = 45)
		{
			DrawCone(position, direction, Gizmos.color, angle);
		}

		public static void DrawCone(Vector3 position, Color color, float angle = 45)
		{
			DrawCone(position, Vector3.up, color, angle);
		}

		public static void DrawCone(Vector3 position, float angle = 45)
		{
			DrawCone(position, Vector3.up, Gizmos.color, angle);
		}

		public static void DrawArrow(Vector3 position, Vector3 direction, Color color, float size = 1f,
			float arrowHeadSize = .33f)
		{
			Color oldColor = Gizmos.color;
			Gizmos.color = color;

			Gizmos.DrawRay(position, direction * size);
			GizmosExt.DrawCone(position + direction * size, -direction * arrowHeadSize, color, 15);

			Gizmos.color = oldColor;
		}

		public static void DrawTwoSidedArrow(Vector3 position, Vector3 direction, Color color, float size = 1f,
			float arrowHeadSize = .33f)
		{
			Color oldColor = Gizmos.color;
			Gizmos.color = color;

			Gizmos.DrawRay(position, direction * size);

			GizmosExt.DrawCone(position + direction * size, -direction * arrowHeadSize, color, 15);
			GizmosExt.DrawCone(position - direction * arrowHeadSize, direction * arrowHeadSize, color, 15);

			Gizmos.color = oldColor;
		}

		public static void DrawArrow(Vector3 position, Vector3 direction)
		{
			DrawArrow(position, direction, Gizmos.color);
		}

		public static void DrawCapsule(Vector3 start, Vector3 end, Color color, float radius = 1)
		{
			Vector3 up = (end - start).normalized * radius;
			Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
			Vector3 right = Vector3.Cross(up, forward).normalized * radius;

			Color oldColor = Gizmos.color;
			Gizmos.color = color;

			float height = (start - end).magnitude;
			float sideLength = Mathf.Max(0, (height * 0.5f) - radius);
			Vector3 middle = (end + start) * 0.5f;

			start = middle + ((start - middle).normalized * sideLength);
			end = middle + ((end - middle).normalized * sideLength);

			//Radial circles
			GizmosExt.DrawCircle(start, up, color, radius);
			GizmosExt.DrawCircle(end, -up, color, radius);

			//Side lines
			Gizmos.DrawLine(start + right, end + right);
			Gizmos.DrawLine(start - right, end - right);

			Gizmos.DrawLine(start + forward, end + forward);
			Gizmos.DrawLine(start - forward, end - forward);

			for (int i = 1; i < 26; i++)
			{

				//Start endcap
				Gizmos.DrawLine(Vector3.Slerp(right, -up, i / 25.0f) + start,
					Vector3.Slerp(right, -up, (i - 1) / 25.0f) + start);
				Gizmos.DrawLine(Vector3.Slerp(-right, -up, i / 25.0f) + start,
					Vector3.Slerp(-right, -up, (i - 1) / 25.0f) + start);
				Gizmos.DrawLine(Vector3.Slerp(forward, -up, i / 25.0f) + start,
					Vector3.Slerp(forward, -up, (i - 1) / 25.0f) + start);
				Gizmos.DrawLine(Vector3.Slerp(-forward, -up, i / 25.0f) + start,
					Vector3.Slerp(-forward, -up, (i - 1) / 25.0f) + start);

				//End endcap
				Gizmos.DrawLine(Vector3.Slerp(right, up, i / 25.0f) + end,
					Vector3.Slerp(right, up, (i - 1) / 25.0f) + end);
				Gizmos.DrawLine(Vector3.Slerp(-right, up, i / 25.0f) + end,
					Vector3.Slerp(-right, up, (i - 1) / 25.0f) + end);
				Gizmos.DrawLine(Vector3.Slerp(forward, up, i / 25.0f) + end,
					Vector3.Slerp(forward, up, (i - 1) / 25.0f) + end);
				Gizmos.DrawLine(Vector3.Slerp(-forward, up, i / 25.0f) + end,
					Vector3.Slerp(-forward, up, (i - 1) / 25.0f) + end);
			}

			Gizmos.color = oldColor;
		}

		public static void DrawCapsule(Vector3 start, Vector3 end, float radius = 1)
		{
			DrawCapsule(start, end, Gizmos.color, radius);
		}

		public static void DrawCross(Vector3 position, float size, Color color)
		{
			Color oldColor = Gizmos.color;
			Gizmos.color = color;

			var pVec = new Vector3(size, size, size);
			var nVec = new Vector3(-size, size, -size);

			Gizmos.DrawLine(position + pVec, position - pVec);
			Gizmos.DrawLine(position + nVec, position - nVec);

			pVec = new Vector3(-size, size, size);
			nVec = new Vector3(size, size, -size);

			Gizmos.DrawLine(position + pVec, position - pVec);
			Gizmos.DrawLine(position + nVec, position - nVec);

			Gizmos.color = oldColor;
		}

		public static void DrawCross(Vector3 position, float size)
		{
			DrawCross(position, size, Gizmos.color);
		}

		public static void DrawQuad(Vector3 middle, Vector2 extents, Vector3 up, Vector3 right)
		{
			var p1 = up * extents.y + right * extents.x;
			var p2 = up * extents.y + right * -extents.x;
			var p3 = up * -extents.y + right * -extents.x;
			var p4 = up * -extents.y + right * extents.x;

			Gizmos.DrawLine(middle + p1, middle + p2);
			Gizmos.DrawLine(middle + p2, middle + p3);
			Gizmos.DrawLine(middle + p3, middle + p4);
			Gizmos.DrawLine(middle + p4, middle + p1);
		}

		public static void DrawQuad(Vector3 middle, Vector2 extents, Vector3 up)
		{
			var v = Vector3.right;

			DrawQuad(middle, extents, up, v);
		}
		
		public static void DrawArc(Vector3 center, Vector3 normal, Vector3 @from, float angle, float radius, 
			int segments = 48) 
		{
			segments = Mathf.Max(1, segments);
 
			//var rad1 = revFactor1 * 2f * Mathf.PI;
			// var rad2 = angle * Mathf.Deg2Rad;
			// var delta = rad2;// - rad1;
			
			var delta = angle * Mathf.Deg2Rad;
 
			var fsegs = (float)segments;
			var inv_fsegs = 1f / fsegs;

			@from.Normalize();
 
			var prevPoint = center + @from * radius;
			var nextPoint = Vector3.zero;
 
			//if(Mathf.Abs(rad1) >= 1E-6f) 
			//	prevPoint = PivotAround(center, normal, vdiff, length, rad1);
 
			for(float seg = 1f; seg <= fsegs; ++seg) 
			{
				nextPoint = PivotAround(center, normal, @from, radius, /*rad1 +*/ delta * seg * inv_fsegs);
				Gizmos.DrawLine(prevPoint, nextPoint);
				prevPoint = nextPoint;
			}
		}
		
		private static Vector3 PivotAround(Vector3 center, Vector3 axis, Vector3 dir, float radius, float radians)
			=> center + radius * (Quaternion.AngleAxis(radians * Mathf.Rad2Deg, axis) * dir);
		
		// WorldAxisAligned
		public static void DrawRoundedRect(Vector3 position, Vector2 extents, float radius, Axis normal)
		{
			if (normal != Axis.X && normal != Axis.Y && normal != Axis.Z)
				normal = Axis.Z;

			radius = Mathf.Max(radius, 0.0f);
			radius = Mathf.Min(radius, extents.x / 2.0f, extents.y / 2.0f);
			
			var innerExtents = new Vector2(Math.Max(0, extents.x - radius), Math.Max(0, extents.y - radius));
			var outerExtents = new Vector2(Math.Max(radius, extents.x), Math.Max(radius, extents.y));
			var inner = new Rect(position.StripValue(normal) - innerExtents, innerExtents*2);

			if (radius > 0)
			{
				Vector3 normalVec = Vector2.zero.FillValue(normal, 1.0f);
				Vector3 verticalVec = Vector3.up;
				Vector3 horizontalVec = Vector3.right;
				switch (normal)
				{
					case Axis.X:
						verticalVec = Vector3.forward;
						horizontalVec = Vector3.Cross(verticalVec, normalVec);
						break;
					case Axis.Y:
						verticalVec = Vector3.left;
						horizontalVec = -Vector3.Cross(verticalVec, normalVec);
						break;
					case Axis.Z:
						verticalVec = Vector3.up;
						horizontalVec = Vector3.Cross(verticalVec, normalVec);
						break;
				}
				const int segmentcount = 8;
				GizmosExt.DrawArc(inner.GetTopLeft().FillValue(normal, position), normalVec, verticalVec, 90, radius, segmentcount);
				GizmosExt.DrawArc(inner.GetTopRight().FillValue(normal, position), normalVec, horizontalVec, 90, radius, segmentcount);
				GizmosExt.DrawArc(inner.GetBottomRight().FillValue(normal, position), normalVec, -verticalVec, 90, radius, segmentcount);
				GizmosExt.DrawArc(inner.GetBottomLeft().FillValue(normal, position), normalVec, -horizontalVec, 90, radius, segmentcount);
			}
         
			var outer = new Rect(position.StripValue(normal) - outerExtents, outerExtents*2);
			
			if (extents.x > radius)
			{
				var tl = new Vector2(inner.xMin, outer.yMax).FillValue(normal, position);
				var tr = new Vector2(inner.xMax, outer.yMax).FillValue(normal, position);
				var br = new Vector2(inner.xMax, outer.yMin).FillValue(normal, position);
				var bl = new Vector2(inner.xMin, outer.yMin).FillValue(normal, position);
				
				Gizmos.DrawLine(tl, tr);
				Gizmos.DrawLine(br, bl);
			}
         
			if (extents.y > radius)
			{
				var tl = new Vector2(outer.xMin, inner.yMax).FillValue(normal, position);
				var tr = new Vector2(outer.xMax, inner.yMax).FillValue(normal, position);
				var br = new Vector2(outer.xMax, inner.yMin).FillValue(normal, position);
				var bl = new Vector2(outer.xMin, inner.yMin).FillValue(normal, position);
				
				Gizmos.DrawLine(tr, br);
				Gizmos.DrawLine(bl, tl);
			}
		}
	}
}