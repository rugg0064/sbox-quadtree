using Sandbox;
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QuadTree
{
	partial class MinimalPlayer : Player
	{
		QuadTree qt;
		float lastDrawTime;

		Vector3 drawLoc;
		private WormsQuadTree wqt;
		private bool[,] noiseMap;
		int mapSize;
		public override void Respawn()
		{
			SetModel( "models/citizen/citizen.vmdl" );

			//
			// Use WalkController for movement (you can make your own PlayerController for 100% control)
			//
			Controller = new WalkController();

			//
			// Use StandardPlayerAnimator  (you can make your own PlayerAnimator for 100% control)
			//
			Animator = new StandardPlayerAnimator();

			//
			// Use ThirdPersonCamera (you can make your own Camera for 100% control)
			//
			Camera = new ThirdPersonCamera();

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;

			base.Respawn();
		}

		[ServerCmd]
		public static void test222()
		{
			for (int i = 0; i < 10; i++)
            {

				int pow2 = 10;
				int size = 1 << pow2;
				int radius = Rand.Int( 1, 2050 );
				int x = Rand.Int( radius, size - radius );
				int y = Rand.Int( radius, size - radius );
				//deleteArea( x, y, radius, Rand.Int( 0, 1 ) % 2 == 0 );
			}
		}

		/*
		[ServerCmd]
		public static void deleteArea(int x1, int y1, int radius, bool status)
		{
			Stopwatch sw = Stopwatch.StartNew();
			int deletedNum = 0;
			for (int x = x1 - radius; x < x1 + radius; x++)
			{
				for ( int y = y1 - radius; y < y1 + radius; y++ )
				{
					if(new Vector2(x,y).Distance(new Vector2(x1, y1)) < radius )
					{
						((MinimalPlayer)ConsoleSystem.Caller.Pawn).qt.setPosition( x, y, status );
						deletedNum++;
					}
				}
			}
			sw.Stop();
			//Log.Info( $"Set {deletedNum} cells in {sw.ElapsedMilliseconds}ms" );
			//Log.Info( $"{deletedNum}, {sw.ElapsedMilliseconds}" );
		}
		*/

		[ServerCmd]
		public static void sdfTest( float x1, float y1, bool value, int radius)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			MinimalPlayer mp = (MinimalPlayer)ConsoleSystem.Caller.Pawn;
			(int, int) lowerLeftBound = ( (int) (x1 - radius), (int) (y1 - radius) );
			(int, int) topRightBound = ( (int) (x1 + radius), (int) (y1 + radius) );


			mp.changeFromSDF( value, lowerLeftBound, topRightBound,  ( position ) =>
			{
				return new Vector2( (float) position.x, (float) position.y ).Distance( new Vector2( x1, y1 ) ) - radius;
			} );
			stopwatch.Stop();
			Log.Info( $"Took {stopwatch.ElapsedMilliseconds} millis to complete" );
		}

		public void changeFromSDF(bool fill, (int x, int y) bottomLeftBound, (int x, int y) topRightBound, Func<(float x, float y), float> sdf)
		{
			//if fill is TRUE, any cell which is PARTIALLY within the SDF is PLACED
			//if fill is FALSE, any cell which is PARTIALLY within the SDF is DELETED
			//partially within is defined sdf(CELL CENTER) < 0
			for ( int x = bottomLeftBound.x; x <= topRightBound.x; x++ )
			{
				for(int y = bottomLeftBound.y; y <= topRightBound.y; y++ )
				{
					if( sdf( (x, y) ) < 0 )
					{
						noiseMap[x, y] = fill;
						//wqt.fixChangedData( (x, y) );
					}
				}
			}
			wqt.updateAccordingToSDF( fill, sdf );
		}

		[ServerCmd]
		public static void setArea( int x1, int y1, bool value, int radius )
		{
			int changeNum = 0;
			Stopwatch sw = Stopwatch.StartNew();
			MinimalPlayer mp = (MinimalPlayer)ConsoleSystem.Caller.Pawn;

			for ( int x = x1 - radius; x < x1 + radius; x++ )
			{
				for ( int y = y1 - radius; y < y1 + radius; y++ )
				{
					if ( new Vector2( x, y ).Distance( new Vector2( x1, y1 ) ) < radius )
					{
						mp.noiseMap[x, y] = value;
						mp.wqt.fixChangedData( (x, y) );
						changeNum++;
					}
				}
			}
			sw.Stop();
			//Log.Info( $"Took {sw.ElapsedMilliseconds} ms" );
			Log.Info( $"Set {changeNum} cells in {sw.ElapsedMilliseconds}ms" );
			//Log.Info( $"{deletedNum}, {sw.ElapsedMilliseconds}" );
		}

		[ServerCmd]
		public static void setPixel(int x1, int y1, bool value)
		{
			Stopwatch sw = Stopwatch.StartNew();
			MinimalPlayer mp = (MinimalPlayer)ConsoleSystem.Caller.Pawn;
			mp.noiseMap[x1, y1] = value;
			mp.wqt.fixChangedData( (x1-1, y1-1) );
			mp.wqt.fixChangedData( (x1-1, y1) );
			mp.wqt.fixChangedData( (x1, y1-1) );
			mp.wqt.fixChangedData( (x1, y1) );
			sw.Stop();
			Log.Info( $"Took {sw.ElapsedMilliseconds} ms" );
			//Log.Info( $"Set {deletedNum} cells in {sw.ElapsedMilliseconds}ms" );
			//Log.Info( $"{deletedNum}, {sw.ElapsedMilliseconds}" );
		}

		/// <summary>
		/// Called every tick, clientside and serverside.
		/// </summary>
		public override void Simulate( Client cl )
		{
			int pow2 = 8;
			int size = 1 << pow2;
			mapSize = size;
			float displayTime = 40.0f;
			Vector3 root = EyePos + EyeRot.Forward * 200;

			base.Simulate( cl );
			SimulateActiveChild( cl, ActiveChild );

			if(IsServer && wqt != null && Time.Now > lastDrawTime + 0.016f)
			{
				lastDrawTime = Time.Now;
				/*
				wqt.display( drawLoc );
				*/

				wqt.doForEachLeafOrNodeOfSize( size >> 1, subTree =>
				{
					var x = subTree.getAllChildrenTriangulation();
					subTree.displayTriangulation( drawLoc, x.vertices, x.indices );
				} );
			}

			if(IsServer && Input.Pressed(InputButton.Reload))
			{
				drawLoc = EyePos + EyeRot.Forward * 100;
				Triangulation.buildTriangulation();
				SimpleSlerpNoise ssn = new SimpleSlerpNoise( Rand.Int( 0, 99999 ), new int[] { 128, 128, 512 }, new float[] { 0.01f, 0.25f, 0.74f } );
				noiseMap = new bool[size + 1, size + 1];
				for ( int x = 0; x < size; x++ )
				{
					for ( int y = 0; y < size; y++ )
					{
						float val = ssn.getValue( x, y );

						val += 1f;
						val /= 2f;
						double a = 0.05;
						double b = 0.90;
						a *= size;
						b *= size;

						double slope = -1 / (b - a);
						double intercept = b / (b - a);
						double comparator = slope * y + intercept;
						comparator = Math.Min( 1, comparator );
						comparator = Math.Max( 0, comparator );

						float xEdgeCutoff = 0.15f;

						double xProbability = (x - 1) / (xEdgeCutoff * size);
						xProbability = Math.Min( xProbability, ((-1 * (x + 1)) + size) / (xEdgeCutoff * size) );
						xProbability = Math.Min( 1, xProbability );

						comparator = Math.Min( comparator, xProbability );

						bool aahh = val < comparator;
						noiseMap[x, y] = aahh;
					}
				}               
				wqt = new WormsQuadTree( null, (0, 0), size, noiseMap );
				Stopwatch sw = Stopwatch.StartNew();
				wqt.freshBuildFromMap();
				sw.Stop();
				Log.Info( $"Building {size}x{size} took: {sw.ElapsedMilliseconds}ms" );
			}

			if (IsServer && Input.Pressed(InputButton.Attack2))
			{
				wqt.doForEachLeafOrNodeOfSize( size >> 4, subTree =>
				{
					subTree.generateMesh( root );
					//var x = subTree.getAllChildrenTriangulation();
					//subTree.displayTriangulation( root, x.vertices, x.indices );
				} );

				/*
				var x = wqt.getAllChildrenTriangulation();
				StringBuilder strb = new StringBuilder();
				x.indicies.ForEach( i => strb.Append(i + ", " ) );
				Log.Info( strb.ToString() );
				Log.Info( x.indicies.Count() );
				*/
				/*
				Triangulation.buildTriangulation();

				for ( int i = 0; i < 1 << 4; i++ )
				{
					int length = Triangulation.indices[i].Length;
					Vector3 offset = root + new Vector3( i, 0, 0 ) * 64;
					for ( int j = 0; j < length; j += 3 )
					{
						int index1 = Triangulation.indices[i][j + 0];
						int index2 = Triangulation.indices[i][j + 1];
						int index3 = Triangulation.indices[i][j + 2];
						DebugOverlay.Line( offset + new Vector3( Triangulation.vertices[i][index1], 0 ) * 32, offset + new Vector3( Triangulation.vertices[i][index2], 0 ) * 32, displayTime );
						DebugOverlay.Line( offset + new Vector3( Triangulation.vertices[i][index2], 0 ) * 32, offset + new Vector3( Triangulation.vertices[i][index3], 0 ) * 32, displayTime );
						DebugOverlay.Line( offset + new Vector3( Triangulation.vertices[i][index3], 0 ) * 32, offset + new Vector3( Triangulation.vertices[i][index1], 0 ) * 32, displayTime );
					}
				}
				*/
			}

			if ( IsServer && Input.Pressed( InputButton.Attack1 ) )
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				for(int i = 0; i < 3; i++)
				{
					int radius = Rand.Int( 1, 32 );
					float x1 = Rand.Float( radius + 1, size - radius - 1 );
					float y1 = Rand.Float( radius + 1, size - radius - 1 );
					bool fill = Rand.Int( 0, 1 ) == 0;
					//sdfTest( x, y, fill, radius );
					(int, int) lowerLeftBound = ((int)(x1 - radius), (int)(y1 - radius));
					(int, int) topRightBound = ((int)(x1 + radius), (int)(y1 + radius));


					this.changeFromSDF( fill, lowerLeftBound, topRightBound, ( position ) =>
					{
						return new Vector2( (float)position.x, (float)position.y ).Distance( new Vector2( x1, y1 ) ) - radius;
					} );
				}
				stopwatch.Stop();
				Log.Info( $"Took {stopwatch.ElapsedMilliseconds} millis to complete" );
				

				/*
				int[,] marchIndicies = new int[size, size];
				for ( int x = 0; x < size - 1; x++ )
				{
					for ( int y = 0; y < size - 1; y++ )
					{
						int index = 0;
						index |= noise[x + 1, y] ? 1 : 0;
						index <<= 1;
						index |= noise[x + 1, y + 1] ? 1 : 0;
						index <<= 1;
						index |= noise[x, y + 1] ? 1 : 0;
						index <<= 1;
						index |= noise[x, y] ? 1 : 0;

						marchIndicies[x, y] = index;
					}
				}
				*/

				//QuadTree quadNoise = new QuadTree( null, 0, 0, size - 1, size - 1, noise );


				//stopwatch.Stop();
				//
				//Log.Info( $"Did everything except display for {size}^2{size*size} size in {stopwatch.ElapsedMilliseconds} millis." );


				/*
				for ( int x = 0; x < size; x++ )
				{
					for ( int y = 0; y < size; y++ )
					{

					}
				}
				*/



				/*
				for(int x = 0; x < size; x++)
				{
					for ( int y = 0; y < size; y++ )
					{
						DebugOverlay.Sphere( root + new Vector3( x, y, 0 ) * 32, 5.0f, noise[x, y] ? Color.Green : Color.Red, false, displayTime );
					}
				}

				for ( int x = 0; x < size - 1; x++ )
				{
					for ( int y = 0; y < size - 1; y++ )
					{
						int index = 0;
						index |= noise[ x + 1, y  ] ? 1 : 0;
						index <<= 1;
						index |= noise[ x + 1, y + 1] ? 1 : 0;
						index <<= 1;
						index |= noise[ x    , y + 1] ? 1 : 0;
						index <<= 1;
						index |= noise[ x    , y    ] ? 1 : 0;

						int i = index;
						int length = Triangulation.indicies[i].Length;
						Vector3 offset = root + new Vector3( x, y, 0 ) * 32;
						for ( int j = 0; j < length; j += 3 )
						{
							int index1 = Triangulation.indicies[i][j + 0];
							int index2 = Triangulation.indicies[i][j + 1];
							int index3 = Triangulation.indicies[i][j + 2];
							DebugOverlay.Line( offset + new Vector3( Triangulation.vertices[i][index1], 0 ) * 32, offset + new Vector3( Triangulation.vertices[i][index2], 0 ) * 32, displayTime );
							DebugOverlay.Line( offset + new Vector3( Triangulation.vertices[i][index2], 0 ) * 32, offset + new Vector3( Triangulation.vertices[i][index3], 0 ) * 32, displayTime );
							DebugOverlay.Line( offset + new Vector3( Triangulation.vertices[i][index3], 0 ) * 32, offset + new Vector3( Triangulation.vertices[i][index1], 0 ) * 32, displayTime );
						}
					}
				}
				*/

				//DualContouring.Test( EyePos + EyeRot.Forward * 100 );

				/*
				QuadTree quadTree = new QuadTree(null, 0, size-1, 0, size -1);

				float seed1 = Rand.Float( 0, 1 );
				float seed2 = Rand.Float( 0, 1 );
				float seed3 = Rand.Float( 0, 1 );
				quadTree.buildFromRandom( (int x, int y) => {
					//float val = ssn.getValue( x, y );

					float val = Noise.Perlin( ((float)x / size) * 2, ((float)y / size) * 2, seed1 ) * 0.5f;
					val += Noise.Perlin( ((float)x / size) * 10, ((float)y / size) * 10, seed2 ) * 0.45f;
					val += Noise.Perlin( ((float)x / size) * 5, ((float)y / size) * 5, seed3 ) * 0.5f;

					val += 1f;
					val /= 2f;
					double a = 0.05;
					double b = 0.90;
					a *= size;
					b *= size;

					double slope = -1 / (b - a);
					double intercept = b / (b - a);
					double comparator = slope * y + intercept;
					comparator = Math.Min( 1, comparator );
					comparator = Math.Max( 0, comparator );

					double xProbability = x / (0.05f * size);
					xProbability = Math.Min( xProbability, ((-1 * x) + size) / (0.05f * size) );
					xProbability = Math.Min( 1, xProbability );

					comparator = Math.Min( comparator, xProbability );

					bool aahh = val < comparator;
					return aahh;
				} );
				//quadTree.setPosition( 0, 0, true );
				qt = quadTree;
				*/
			}
}

		public override void OnKilled()
		{
			base.OnKilled();
			EnableDrawing = false;
		}
	}
}
