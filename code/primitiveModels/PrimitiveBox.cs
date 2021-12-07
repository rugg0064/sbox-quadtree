using System;
using System.Collections.Generic;
using Sandbox;

namespace QuadTree
{
	public partial class PrimitiveBox : BasePrimitive
	{
		public override Model BuildModel()
		{
			var mesh = new Mesh( Material.Load( "materials/dev/reflectivity_30.vmat" ) );
			BuildMesh( mesh );

			var model = Model.Builder
				.AddMesh( mesh )
				.AddCollisionBox( Size / 2 )
				.Create();

			return model;
		}

		public override void BuildMesh( Mesh mesh )
		{
			var positions = new Vector3[]
			{
				new Vector3(-0.5f, -0.5f, 0.5f) * Size,
				new Vector3(-0.5f, 0.5f, 0.5f) * Size,
				new Vector3(0.5f, 0.5f, 0.5f) * Size,
				new Vector3(0.5f, -0.5f, 0.5f) * Size,
				new Vector3(-0.5f, -0.5f, -0.5f) * Size,
				new Vector3(-0.5f, 0.5f, -0.5f) * Size,
				new Vector3(0.5f, 0.5f, -0.5f) * Size,
				new Vector3(0.5f, -0.5f, -0.5f) * Size,
			};

			var faceIndices = new int[]
			{
				0, 1, 2, 3,
				7, 6, 5, 4,
				0, 4, 5, 1,
				1, 5, 6, 2,
				2, 6, 7, 3,
				3, 7, 4, 0,
			};

			var uAxis = new Vector3[]
			{
				Vector3.Forward,
				Vector3.Left,
				Vector3.Left,
				Vector3.Forward,
				Vector3.Right,
				Vector3.Backward,
			};

			var vAxis = new Vector3[]
			{
				Vector3.Left,
				Vector3.Forward,
				Vector3.Down,
				Vector3.Down,
				Vector3.Down,
				Vector3.Down,
			};

			List<SimpleVertex> verts = new();
			List<int> indices = new();

			for ( var i = 0; i < 6; ++i )
			{
				var tangent = uAxis[i];
				var binormal = vAxis[i];
				var normal = Vector3.Cross( tangent, binormal );

				for ( var j = 0; j < 4; ++j )
				{
					var vertexIndex = faceIndices[(i * 4) + j];
					var pos = positions[vertexIndex];

					verts.Add( new SimpleVertex()
					{
						position = pos,
						normal = normal,
						tangent = tangent,
						texcoord = Planar( (Origin + pos) / 32, uAxis[i], vAxis[i] )
					} );
				}

				indices.Add( i * 4 + 0 );
				indices.Add( i * 4 + 2 );
				indices.Add( i * 4 + 1 );
				indices.Add( i * 4 + 2 );
				indices.Add( i * 4 + 0 );
				indices.Add( i * 4 + 3 );
			}

			mesh.CreateVertexBuffer<SimpleVertex>( verts.Count, SimpleVertex.Layout, verts.ToArray() );
			mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );
		}
	}
}
