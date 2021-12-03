using Sandbox;
using System;
using System.Linq;
using System.Diagnostics;
namespace MinimalExample
{
	partial class MinimalPlayer : Player
	{
		QuadTree qt;
		float lastDrawTime;
		Vector3 drawLoc;
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

				int pow2 = 12;
				int size = 1 << pow2;
				int radius = Rand.Int( 1, 2050 );
				int x = Rand.Int( radius, size - radius );
				int y = Rand.Int( radius, size - radius );
				deleteArea( x, y, radius, Rand.Int( 0, 1 ) % 2 == 0 );
			}

		}

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
			Log.Info( $"{deletedNum}, {sw.ElapsedMilliseconds}" );
		}

		/// <summary>
		/// Called every tick, clientside and serverside.
		/// </summary>
		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			//
			// If you have active children (like a weapon etc) you should call this to 
			// simulate those too.
			//
			SimulateActiveChild( cl, ActiveChild );

			if(Time.Now > lastDrawTime + 1 && qt != null)
			{
				int pow2 = 12;
				int size = 1 << pow2;
				qt.display( drawLoc, (float)1024 / (1 << pow2) );
				lastDrawTime = Time.Now;
			}

			//
			// If we're running serverside and Attack1 was just pressed, spawn a ragdoll
			//
			if(IsServer && Input.Pressed(InputButton.Attack2))
			{
				drawLoc = EyePos + EyeRot.Forward * 2500;
			}
			if ( IsServer && Input.Pressed( InputButton.Attack1 ) )
			{
				SimpleSlerpNoise ssn = new SimpleSlerpNoise( Rand.Int( 0, 99999 ), new int[] { 32, 64, 256 }, new float[] { 0.01f, 0.25f, 0.74f } );
				int pow2 = 12;
				int size = 1 << pow2;
				QuadTree quadTree = new QuadTree(null, 0, size-1, 0, size -1);

				/*
				for(int x = 0; x < size; x++)
				{
					for(int y = 0; y < size; y++)
					{
						float val = ssn.getValue( x, y );
						
						val += 1f;
						val /= 2f;

						//A = the percentage where everything under it is completely solid
						//B = the percentage where everything above it is completely air
						double a = 0.05;
						double b = 0.90;

						a *= size;
						b *= size;
						double slope = -1 / (b - a);
						double intercept = b / (b - a);
						double comparator = slope*y + intercept;
						comparator = Math.Min( 1, comparator );
						comparator = Math.Max( 0, comparator );
						

						double xProbability = x / (0.05f * size);
						xProbability = Math.Min(xProbability,  ((-1 * x) + size) / (0.05f * size));
						xProbability = Math.Min( 1, xProbability );

						comparator = Math.Min( comparator, xProbability );

						if(x == 0)
						{
							Log.Info( comparator );
						}
						bool aahh = val < comparator;
						quadTree.setPosition( x, y, aahh );
					}
				}
				*/
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
				//
			}
		}

		public override void OnKilled()
		{
			base.OnKilled();

			EnableDrawing = false;
		}
	}
}
