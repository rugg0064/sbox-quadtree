using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuadTree
{
    public static class DualContouring
	{
		private class SuperVector2
		{
			public Vector2 vector;
			public bool[] components = new bool[4];
		}

		public static (int x, int y)[] intOffsets = new (int, int)[]
		{
			(0, 0),
			(0, 1),
			(1, 1),
			(1, 0)
		};

		public static Vector2[] nodeVerticies = new Vector2[]
		{
			new Vector2(0, 0),
			new Vector2(0, 1),
			new Vector2(1, 1),
			new Vector2(1, 0)
		};


		private static SuperVector2 getVertex(int x, int y, float[,] values)
		{
			Log.Info( $"New value: {x} {y}" );
			List<Vector2> vertices = new List<Vector2>();
			bool[] components = new bool[4];
			for(int i = 0; i < 4; i++)
			{
				int thisIndex = i;
				int nextIndex = i == 3 ? 0 : i + 1;

				Log.Info( $"{thisIndex} {nextIndex}" );

				(int x, int y) thisIndexOffset = intOffsets[thisIndex];
				float thisValue = values[x + thisIndexOffset.x, y + thisIndexOffset.y];

				(int x, int y) nextIndexOffset = intOffsets[nextIndex];
				float otherValue = values[x + nextIndexOffset.x, y + nextIndexOffset.y];

				Log.Info( $"({x + thisIndexOffset.x}, {y + thisIndexOffset.y}) ({x + nextIndexOffset.x}, {y + nextIndexOffset.y})" );
				if (MathF.Sign(thisValue) != MathF.Sign( otherValue ) )
				{
					float lerpAmnt = ((thisValue + otherValue) + 2) / 4;
					Vector2 vectorToADd = Vector2.Lerp( nodeVerticies[thisIndex], nodeVerticies[nextIndex], lerpAmnt );
					//Log.Info( $"Adding {vectorToADd}" );
					vertices.Add( vectorToADd );
					components[i] = true;
				}
			}

			if(vertices.Count == 0)
			{
				return null;
			}
			else
			{
				Vector3 returnVector = Vector3.Zero;
				foreach (Vector3 v in vertices)
				{
					returnVector += v;
				}
				SuperVector2 outVector = new SuperVector2();
				outVector.vector = returnVector / vertices.Count;
				outVector.components = components;

				return outVector;
			}
		}




		public static void Test(Vector3 rootPos)
		{
			/*
			float[,] values = new float[,]
			{
				{ -1, -1, -1, -1, -1 },
				{ -1, -1,  1, -1, -1 },
				{ -1,  1,  1,  1, -1 },
				{ -1, -1,  1, -1, -1 },
				{ -1, -1, -1, -1, -1 } //A little + sign
			};
			*/
			SimpleSlerpNoise ssn = new SimpleSlerpNoise( Sandbox.Rand.Int( 0, 99999 ), new int[] { 1, 4, 8 }, new float[] { 0.01f, 0.25f, 0.74f } );

			int size = 1 << 5;
			float[,] values = new float[size, size];
			for(int x = 0; x < values.GetLength(0); x++)
			{
				for ( int y = 0; y < values.GetLength( 1 ); y++ )
				{
					values[x,y] = ssn.getValue( x, y );
				}
			}

			/*
			float[,] values = new float[,]
			{
				{ -1, -1, -1 },
				{ -1,  1, -1 },
				{ -1, -1, -1 }
			};
			*/

			//DebugOverlay.Sphere( rootPos, 0.25f, Color.White, false, 5.0f );

			SuperVector2[,] vertices = new SuperVector2[values.GetLength( 0 ) - 1, values.GetLength( 1 ) - 1];
			for(int x = 0; x < vertices.GetLength( 0 ); x++)
			{
				for ( int y = 0; y < vertices.GetLength( 1 ); y++ )
				{
					vertices[x, y] = getVertex( x, y, values );
				}
			}

			for ( int x = 0; x < values.GetLength( 0 ); x++ )
			{
				for ( int y = 0; y < values.GetLength( 1 ); y++ )
				{
					Vector3 worldPos = rootPos + new Vector3( x, y, 0 ) * 64;
					DebugOverlay.Sphere( worldPos, 3.0f, values[x,y] <= 0 ? Color.Yellow : Color.Red, false, 5.0f );
				}
			}
			
			/*
			for ( int x = 0; x < vertices.GetLength( 0 ); x++ )
			{
				for ( int y = 0; y < vertices.GetLength( 1 ); y++ )
				{
					SuperVector2 thisVertex = vertices[x, y];
					if ( thisVertex != null )
					{
						Vector2 thisVector = vertices[x, y].vector;
						Vector3 thisVertexWorldPosition = rootPos + new Vector3( x, y, 0 ) * 64 + new Vector3( thisVector, 0 ) * 64;
						DebugOverlay.Sphere( thisVertexWorldPosition, 5f, Color.White, false, 5.0f );
					}
				}
			}*/

			for ( int x = 0; x < vertices.GetLength( 0 ); x++ )
			{
				for ( int y = 0; y < vertices.GetLength( 1 ); y++ )
				{
					SuperVector2 thisSuperVector = vertices[x, y];
					if(thisSuperVector == null)
					{
						continue;
					}
					Vector2 thisVertex = thisSuperVector.vector;
					Vector3 thisVertexWorldPosition = rootPos + new Vector3( x, y, 0 ) * 64 + new Vector3(thisVertex, 0) * 64;


					if( x != vertices.GetLength( 0 )  - 1 )
					{
						SuperVector2 rightSuperVertex = vertices[x + 1, y];
						if( rightSuperVertex != null && rightSuperVertex.components[0])
						{
							Vector2 rightVertex = rightSuperVertex.vector;
							Vector3 rightVertexWorldPosition = rootPos + new Vector3( x + 1, y, 0 ) * 64 + new Vector3( rightVertex, 0 ) * 64;
							if ( rightVertex.x >= 0 )
							{
								DebugOverlay.Line( thisVertexWorldPosition, rightVertexWorldPosition, 5.0f );
							}
						}
					}

					if ( y != vertices.GetLength( 1 ) - 1 )
					{
						SuperVector2 upSuperVertex = vertices[x, y + 1];
						if ( upSuperVertex != null && upSuperVertex.components[3] )
						{
							Vector2 upVertex = upSuperVertex.vector;
							Vector3 upVertexWorldPosition = rootPos + new Vector3( x, y + 1, 0 ) * 64 + new Vector3( upVertex, 0 ) * 64;
							if ( upVertex.x >= 0 )
							{
								DebugOverlay.Line( thisVertexWorldPosition, upVertexWorldPosition, 5.0f );
							}
						}
					}
				}
			}
		}
	}
}
