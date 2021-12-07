using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace QuadTree
{
	public class WormsQuadTree
	{
		//Each node, at its smallest level, is an instance of the marching cubes algorithm.
		//This data structure compresses filled and empty results (when the algo computes 1111 or 0000)
		//If it is neither 1111 nor 0000, then it will be a MIX

		//Each node is directly tied to an entity.
		private Entity marchEntity;

		//Represents the data of a node
		public enum DataState
		{ 
			FILLED,
			EMPTY,
			MIX,
			INTERMEDIATE
		}

		private DataState state;
		private (int x, int y) position;
		private int size;
		private WormsQuadTree parent;
		private WormsQuadTree[] children;
		private int marchingSquaresIndex;
		private bool[,] map;
		private (int x, int y) midpoint;

		public WormsQuadTree(WormsQuadTree parent, (int x, int y) position, int size, bool[,] map)
		{
			this.state = DataState.EMPTY;
			this.position = position;
			this.size = size;
			this.map = map;
			this.midpoint = (position.x + (size / 2), position.y + (size / 2));
		}

		private void deleteAllEntities()
		{
			if(this.marchEntity != null && this.marchEntity.IsValid)
			{
				this.marchEntity.Delete();
			}
			if(this.state == DataState.INTERMEDIATE)
			{
				foreach(WormsQuadTree child in children)
				{
					child.deleteAllEntities();
				}
			}
		}

		private static int getIndexFromState(DataState state)
		{
			if(state == DataState.FILLED)
			{
				return 0b1111;
			}
			return 0;
		}

		public void freshBuildFromMap()
		{
			//Log.Info( "Building" );
			children = null;
			if ( size == 1 )
			{
				marchingSquaresIndex = getMarchingSquaresIndex( position, map );
				//Log.Info( $"1x1 with index {index}" );
				state = getDataState( marchingSquaresIndex );
			}
			else
			{
				this.state = DataState.INTERMEDIATE;
				//Log.Info( $"Splitting size {size} into {position} {midpoint}" );
				createChildren();
				//Log.Info( "Recursing" );
				foreach ( WormsQuadTree child in children )
				{
					child.freshBuildFromMap();
				}
				//Log.Info( "Finished recurse" );
				compressNode();
			}
		}

		private void compressNode()
		{
			(bool allSame, DataState type) childrenSameResult = childrenAllSame();

			if ( childrenSameResult.allSame && (childrenSameResult.type == DataState.FILLED || childrenSameResult.type == DataState.EMPTY) )
			{
				//Log.Info( "Children all the same; becoming same state as child" );
				state = childrenSameResult.type;
				children = null;
				marchingSquaresIndex = getIndexFromState( state );
			}
		}

		private (bool allSame, DataState type) childrenAllSame()
		{
			bool allSame = true;
			DataState childState = children[0].state;
			for ( int i = 1; i < children.Length && allSame; i++ )
			{
				if ( children[i].state != childState )
				{
					allSame = false;
				}
			}
			return (allSame, childState);
		}

		//This assumes that any pre-existing children have been gracefully cleaned up!
		private void createChildren()
		{
			children = new WormsQuadTree[4];
			children[0] = new WormsQuadTree( this, (position.x, position.y), size / 2, map );
			children[1] = new WormsQuadTree( this, (position.x, midpoint.y), size / 2, map );
			children[2] = new WormsQuadTree( this, (midpoint.x, position.y), size / 2, map );
			children[3] = new WormsQuadTree( this, (midpoint.x, midpoint.y), size / 2, map );
		}

		public void updateAccordingToSDF( bool fill, Func<(float x, float y), float> sdf )
		{
			if (size == 1)
			{
				marchingSquaresIndex = getMarchingSquaresIndex( this.position, map );
				state = getDataState( marchingSquaresIndex );
				return;
			}

			float radius = size/2 * MathF.Sqrt( 2 );
			float thisX = position.x + 1 + size/2;
			float thisY = position.y + 1 + size/2;
			float distance = sdf( (thisX, thisY) );

			//How do we check if we are definitely completely outside of the SDF??????

			/*
			if(distance < radius * -1)
			{ //We are entirely within the SDF, the function definitely changed this entire node!
				this.state = fill ? DataState.FILLED : DataState.EMPTY;
				children = null;
			}
			else
			*/
			{ //We aren't sure what happened, so recurse down.
				if(state != DataState.INTERMEDIATE)
				{
					createChildren();
				}
				state = DataState.INTERMEDIATE;
				foreach(WormsQuadTree child in children)
				{
					child.updateAccordingToSDF( fill, sdf );
				}
				compressNode();
			}
		}

		private WormsQuadTree getChild((int x, int y) position)
		{
			if(position.x < midpoint.x)
			{
				if(position.y < midpoint.y)
				{
					return children[0];
				}
				else
				{
					return children[1];
				}
			}
			else
			{
				if ( position.y < midpoint.y )
				{
					return children[2];
				}
				else
				{
					return children[3];
				}
			}
		}
		private static DataState getDataState( int index )
		{
			switch ( index )
			{
				case 0b0000:
					return DataState.EMPTY;
				case 0b1111:
					return DataState.FILLED;
				default:
					return DataState.MIX;
			}
		}

		public void fixChangedData( (int x, int y) position )
		{
			//Pass in the MAP INDEX that was modified.
			fixChangedDataStep2( (position.x - 1, position.y - 1) );
			fixChangedDataStep2( (position.x - 1, position.y) );
			fixChangedDataStep2( (position.x, position.y - 1) );
			fixChangedDataStep2( (position.x, position.y) );
		}

		private void fixChangedDataStep2( (int x, int y) internalPosition )
		{
			if ( position.x < 0 || position.x >= this.position.x + size ||
			position.y < 0 || position.y >= this.position.y + size )
			{
				return;
			}
			else
			{
				fixChangedDataInternal( internalPosition );
			}
		}

		private void fixChangedDataInternal( (int x, int y) position )
		{
			if (size == 1)
			{
				int index = this.marchingSquaresIndex;
				marchingSquaresIndex = getMarchingSquaresIndex( this.position, map );
				state = getDataState( marchingSquaresIndex );
				//Log.Info( $"{position} {this.position} Old index {index} new index {marchingSquaresIndex} {state}" );
			}
			else
			{
				if(state != DataState.INTERMEDIATE)
				{
					createChildren();
					foreach( WormsQuadTree child in children )
					{
						child.state = this.state;
					}
					this.state = DataState.INTERMEDIATE;
				}
				WormsQuadTree correctChild = getChild( position );
				//Log.Info($"Looking for {position} This {this.position} {this.size} looking for {position} found {correctChild} {correctChild.position} {correctChild.size}");
				correctChild.fixChangedDataInternal( position );
				compressNode();
			}
			//Log.Info( $"{position} {size} {state}" );
		}

		public void display(Vector3 root)
		{
			switch(state)
			{
				case DataState.FILLED:
					displayMarchingCubes( root, position, 0b1111, size );
					break;
				case DataState.EMPTY:
					break;
				case DataState.MIX:
					displayMarchingCubes( root, position, marchingSquaresIndex, size );
					break;
				case DataState.INTERMEDIATE:
					//Log.Info( $"Recursing for {position} size {size}" );
					foreach(WormsQuadTree child in children)
					{
						child.display( root );
					}
					break;
			}
		}

		public void doForEachLeafOrNodeOfSize(int size, Action<WormsQuadTree> action)
		{
			if(this.size == size)
			{
				action( this );
			}
			else
			{
				if(this.children == null)
				{
					action( this );
				}
				else if(this.size > size)
				{
					foreach (WormsQuadTree child in children)
					{
						child.doForEachLeafOrNodeOfSize( size, action );
					}
				}
			}
		}

		public (List<Vector2> vertices, List<int> indices) getAllChildrenTriangulation()
		{
			if(state != DataState.INTERMEDIATE)
			{
				return getTriangulation( position );
			}
			else
			{ //Node has four children
			  //Recurse onto them
				List<Vector2> vertices = new List<Vector2>();
				List<int> indices = new List<int>();
				int startPos = 0;
				foreach(WormsQuadTree child in children)
				{
					(List<Vector2> vertices, List<int> indices) result = child.getAllChildrenTriangulation();

					(int x, int y) childPosition = child.position;
					Vector2 difference = new Vector2( childPosition.x - this.position.x, childPosition.y - this.position.y );

					result.vertices.ForEach( v => vertices.Add( v + difference ) );
					result.indices.ForEach( i => indices.Add( i + startPos ) );
					startPos += result.vertices.Count();
				}
				return (vertices, indices);
			}
		}


		private (List<Vector2> vertices, List<int> indices) getTriangulation((int x, int y) root)
		{
			List<int> indices = new List<int> (Triangulation.indices[marchingSquaresIndex]);
			List<Vector2> vertices = new List<Vector2> (Triangulation.vertices[marchingSquaresIndex]);

			for(int i = 0; i < vertices.Count; i++)
			{
				vertices[i] = vertices[i] * size;
			}

			return (vertices, indices);
		}

		private static int getMarchingSquaresIndex((int x, int y) position, bool[,] map)
		{
			int index = 0;
			index |= map[position.x + 1, position.y] ? 1 : 0;
			index <<= 1;
			index |= map[position.x + 1, position.y + 1] ? 1 : 0;
			index <<= 1;
			index |= map[position.x, position.y + 1] ? 1 : 0;
			index <<= 1;
			index |= map[position.x, position.y] ? 1 : 0;
			return index;
		}

		public string ToString()
		{
			return $"pos: {position} size: {size} index: {marchingSquaresIndex} state: {state}";
		}

		public void displayTriangulation(Vector3 root, List<Vector2> vertices, List<int> indices)
		{
			//Log.Info( this.ToString() );
			//Log.Info( $"Number of verts: {vertices.Count()}" );
			//Log.Info( $"Number of indices: {indices.Count()}" );

			float displayTime = 0.016f;
			Vector3 offset = root + new Vector3( position.x, position.y, 0 ) * 32;
			Color color = Color.Random;
			for (int i = 0; i < indices.Count; i+=3 )
			{
				//Log.Info( $"I: {i}" );
				//Log.Info( $"index[i]: {indices[i]}" );
				//Log.Info( $"index[i+1]: {indices[i+1]}" );
				//Log.Info( $"index[i+2]: {indices[i+2]}" );
				Vector3 a = offset + (new Vector3( vertices[indices[i]], 0) * 32);
				Vector3 b = offset + (new Vector3( vertices[indices[i+1]], 0) * 32);
				Vector3 c = offset + (new Vector3( vertices[indices[i+2]], 0) * 32);

				DebugOverlay.Line( a, b, color, displayTime, true );
				DebugOverlay.Line( b, c, color, displayTime, true );
				DebugOverlay.Line( c, a, color, displayTime, true );
			}
		}

		public void generateMesh( Vector3 root )
		{
			(List<Vector2> vertices, List<int> indices) triangulation = getAllChildrenTriangulation();
			Vector3 offset = root + new Vector3( position.x, position.y, 0 ) * 32;

			if (triangulation.vertices.Count > 0)
			{
				Mesh mesh = new Mesh( Material.Load( "materials/dev/reflectivity_30.vmat" ) );
				SimpleVertex[] simpleVertices = new SimpleVertex[triangulation.vertices.Count];
				for(int i = 0; i < triangulation.vertices.Count; i++)
				{
					simpleVertices[i] = new SimpleVertex()
					{
						position = offset + (new Vector3( triangulation.vertices[i], 0 )),
						normal = Vector3.Up,
						tangent = Vector3.Forward,
						texcoord = Vector3.Zero
					};
				}
				mesh.CreateVertexBuffer<SimpleVertex>( triangulation.vertices.Count, SimpleVertex.Layout );
				mesh.SetVertexBufferData<SimpleVertex>( simpleVertices );
				mesh.CreateIndexBuffer( triangulation.indices.Count, triangulation.indices );

				Vector3[] collisionVertices = new Vector3[triangulation.vertices.Count()];
				for(int i = 0; i < triangulation.vertices.Count; i++)
				{
					collisionVertices[i] = new Vector3( triangulation.vertices[i], 0 ) * 32;
				}

				Model model = new ModelBuilder()
					.AddMesh( mesh )
					.AddCollisionMesh( collisionVertices, triangulation.indices.ToArray() )
					.Create();
				ModelEntity modelEntity = new ModelEntity();
				modelEntity.SetModel( model );
				modelEntity.Position = root + new Vector3(position.x, position.y, 0) * 32;
				modelEntity.SetupPhysicsFromModel( PhysicsMotionType.Static );
				modelEntity.Spawn();
				modelEntity.Transmit = TransmitType.Always;
				//DebugOverlay.Sphere( modelEntity.Position, 10.0f, Color.Green, false, 10.0f );

				/*
				mesh.SetVertexBufferData<Vertex>( new Span<Vertex>( verticies.ToArray() ) );
				mesh.SetVertexRange( 0, numTris );
				Model model = new ModelBuilder()
					.AddMesh( mesh )
					.AddCollisionMesh( collisionVerticies.ToArray(), indicies )
					.WithMass( 10 )
					.Create();
				ModelEntity e = new ModelEntity();
				e.SetModel( model );
				e.Position = position;
				e.SetupPhysicsFromModel( PhysicsMotionType.Static );
				e.Spawn();
				return e;
				*/
			}
		}

		private static void displayMarchingCubes( Vector3 root, (int x, int y) position, int index, int size)
		{
			float displayTime = 0.016f;
			int i = index;
			int length = Triangulation.indices[i].Length;
			Vector3 offset = root + new Vector3( position.x, position.y, 0 ) * 32;
			for ( int j = 0; j < length; j += 3 )
			{
				int index1 = Triangulation.indices[i][j + 0];
				int index2 = Triangulation.indices[i][j + 1];
				int index3 = Triangulation.indices[i][j + 2];
				DebugOverlay.Line( offset + new Vector3( Triangulation.vertices[i][index1], 0 ) * 32 * size, offset + new Vector3( Triangulation.vertices[i][index2], 0 ) * 32 * size, displayTime );
				DebugOverlay.Line( offset + new Vector3( Triangulation.vertices[i][index2], 0 ) * 32 * size, offset + new Vector3( Triangulation.vertices[i][index3], 0 ) * 32 * size, displayTime );
				DebugOverlay.Line( offset + new Vector3( Triangulation.vertices[i][index3], 0 ) * 32 * size, offset + new Vector3( Triangulation.vertices[i][index1], 0 ) * 32 * size, displayTime );
			}
		}
	}
}
