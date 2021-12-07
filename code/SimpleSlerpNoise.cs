using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace QuadTree
{
	//This is the class I am using to generate noise values.
	public class SimpleSlerpNoise
	{
		int seed;
		int octaves;
		int[] valuesPerGrid;
		float[] percentagesPerOctave;
		public SimpleSlerpNoise( int seed, int[] valuesPerGrid, float[] percentagesPerOctave)
		{
			if ( valuesPerGrid.Length != percentagesPerOctave.Length )
			{
				throw new Exception( "Array sizes must be equal" );
			}
			this.seed = seed;
			this.octaves = valuesPerGrid.Length;
			this.valuesPerGrid = valuesPerGrid;
			this.percentagesPerOctave = percentagesPerOctave;
		}
		public float getValue(int x, int y)
		{
			float accumulation = 0;
			for ( int octaveNum = 0; octaveNum < octaves; octaveNum++ )
			{
				int octaveValuesPerGrid = valuesPerGrid[octaveNum];
				int gridX = x / octaveValuesPerGrid;
				int gridY = y / octaveValuesPerGrid;

				int val00 = getGridValue( gridX, gridY );
				int val10 = getGridValue( gridX + 1, gridY );
				int val01 = getGridValue( gridX, gridY + 1 );
				int val11 = getGridValue( gridX + 1, gridY + 1 );

				float offsetX = sCurve( (x - (gridX * octaveValuesPerGrid)) / (float)octaveValuesPerGrid );
				float offsetY = sCurve( (y - (gridY * octaveValuesPerGrid)) / (float)octaveValuesPerGrid );
				float inverseOffsetX = 1 - offsetX;
				float inverseOffsetY = 1 - offsetY;

				float val0 = (val00 * inverseOffsetX) + (val10 * offsetX);
				float val1 = (val01 * inverseOffsetX) + (val11 * offsetX);

				float val = (val0 * inverseOffsetY) + (val1 * offsetY);

				//This value will be between [-2147m, 2147m]
				val /= int.MaxValue;
				//Convert to  [-1,1]
				accumulation += val * percentagesPerOctave[octaveNum];
			}
			return accumulation;
		}

		private int getGridValue(int gridX, int gridY)
		{
			return lcg( getPositionalSeed( gridX, gridY ) );
		}

		public int lcg( int seed )
		{
			//Random prime numbers seem to work
			//timer.Start();
			int val = lcg( 102191, 0, 3, seed );
			//timer.Stop();
			return val;
		}

		//Linear congruent generator
		public int lcg( int a, int c, int n, int n0 )
		{
			if ( n == 0 )
			{
				return n0;
			}
			else if(c == 0)
			{
				//According to one site (which I need to come back and grab)
				//this formula can be used to collapse the recursive structure
				//as long as c is 0. Based on some tests it seems to be 
				//random enough for these purposes.
				int an = 1;
				for(int i = 0; i < n; i++ )
				{
					an *= a;
				}
				return an * n0;
			}
			else
			{
				return ((a * lcg( a, c, n - 1, n0 )) + c);
			}
		}

		//For inputs between x=[0,1] returns a smooth curve between y=[0,1]
		//With x=0 and x=1 having slopes of zero
		public float sCurve( float x )
		{
			return (1 * ((x * (x * 6.0f - 15.0f) + 10.0f) * x * x * x));
		}

		//Good for getting different values for each position,
		//but positions that are close on some axis get similar values,
		//so for randomness the LCG must be used as well. with this as a seed.
		private int getPositionalSeed( int x, int y )
		{
			/*
			//Used to be an FNV-1 Hash, seems like this new system is fine.
			//It will absolutely repeat eventually, but oh well.
			//Multiplications were very slow.
			timer.Start();
			uint h = 2166136261;
			h *= 16777619;
			h ^= (uint)seed; 
			h *= 16777619;
			h ^= (uint)x;
			h *= 16777619;
			h ^= (uint)y;
			h *= 16777619;
			h ^= (uint)z;
			timer.Stop();
			return (int)h;
			*/
			uint h = 2166136261;
			h ^= (uint)seed;
			h ^= (uint) ((x << 0 ) | (x << (32 - 0 )));
			h ^= (uint) ((y << 16) | (y << (32 - 16)));
			return (int)h;
		}
	}
}
