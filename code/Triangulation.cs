using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuadTree
{
	public static class Triangulation
	{
		public readonly static Vector2[] nodeVerticies = new Vector2[]
		{
				new Vector2(0, 0),
				new Vector2(0, 1),
				new Vector2(1, 1),
				new Vector2(1, 0)
		};

		public static Vector2[][] vertices { get; private set; }
		public static int[][] indices { get; private set; }

		private static List<Vector2> getCircularVectors(int corners)
		{
			bool firstBit = (corners & 1) == 1;
			List<Vector2> vectors = new List<Vector2>();

			for ( int i = 0; i < 4; i++ )
			{
				bool set = (corners & 1) == 1;
				if ( set )
				{
					vectors.Add( nodeVerticies[i] );
				}

				bool nextIsSet;
				if ( i == 3 )
				{
					nextIsSet = firstBit;
				}
				else
				{
					nextIsSet = ((corners >> 1) & 1) == 1;
				}

				if ( set != nextIsSet )
				{
					int nextIndex = i == 3 ? 0 : i + 1;
					vectors.Add( (nodeVerticies[i] + nodeVerticies[nextIndex]) / 2 );
				}
				corners >>= 1;
			}

			return vectors;
		}

		public static void buildTriangulation()
		{
			triangulateConvexPolygon( getCircularVectors( 0b1101 ) );

			vertices = new Vector2[1 << 4][];
			indices = new int[1 << 4][];

			//MSB to LSB (reverse from above)
			//bottom-right
			//top-right
			//top-left
			//bottom-left

			int state = 0;
			do
			{
				//Log.Info( Convert.ToString( state, 2 ).PadLeft( 4, '0' ) );

				List<Vector2> vectors = getCircularVectors( state );
				Triangulation.vertices[state] = vectors.ToArray();
				Triangulation.indices[state] = triangulateConvexPolygon( vectors ).ToArray();

				state++;
			} while ( state <= 0b1111 );
		}

		public static List<int> triangulateConvexPolygon( List<Vector2> vertices )
		{
			List<int> vertexIndices = new List<int>();
			(double weight, List<int> vertexIndices) result = mwt( 0, vertices.Count - 1, vertices );
			return result.vertexIndices;
		}

		private static (double weight, List<int> vertexIndices) mwt(int i, int j, List<Vector2> vertices)
		{
			if(j >= i + 2)
			{
				float ijWeight = vertices[i].Distance(vertices[j]);

				(double weight, List<int> vertices) minimumWeightSet = (double.PositiveInfinity, null);
				
				for(int k = i + 1; k < j; k++)
				{
					double thisTriangleWeight = vertices[i].Distance( vertices[k] ) + vertices[k].Distance( vertices[j] ) + vertices[j].Distance( vertices[i] );
					( double weight, List<int> vertexIndices ) ikRecursion = mwt( i, k, vertices );

					( double weight, List<int> vertexIndices ) kjRecursion = mwt( k, j, vertices );

					double totalWeight = thisTriangleWeight + ikRecursion.weight + kjRecursion.weight;
					if(totalWeight <= minimumWeightSet.weight)
					{
						List<int> newIndices = new List<int>() { i, k, j };
						newIndices.AddRange( ikRecursion.vertexIndices );
						newIndices.AddRange( kjRecursion.vertexIndices );

						minimumWeightSet = (totalWeight, newIndices);
					}
				}
				return minimumWeightSet;
			}
			else
			{
				return (0, new List<int>());
			}
		}
	}
}
