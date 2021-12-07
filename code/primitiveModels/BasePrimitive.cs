using Sandbox;

namespace QuadTree
{
	public partial class BasePrimitive : BaseNetworkable
	{
		[Net] public PrimitiveEntity Entity { get; set; }
		[Net] public Vector3 Origin { get; set; }
		[Net, Change( nameof( DirtyEntity ) )] public Vector3 Size { get; set; }

		/// <summary>
		/// Tell our entity the primitive is dirty and needs rebuilding.
		/// </summary>
		protected void DirtyEntity()
		{
			Entity?.CreateMesh();
		}

		public virtual void BuildMesh( Mesh mesh ) { }
		public virtual Model BuildModel() { return null; }
		public virtual void DrawDebug( Vector3 origin ) { }

		protected static Vector2 Planar( Vector3 pos, Vector3 uAxis, Vector3 vAxis )
		{
			return new Vector2()
			{
				x = Vector3.Dot( uAxis, pos ),
				y = Vector3.Dot( vAxis, pos )
			};
		}

		protected static void GetTangentBinormal( Vector3 normal, out Vector3 tangent, out Vector3 binormal )
		{
			var t1 = Vector3.Cross( normal, Vector3.Forward );
			var t2 = Vector3.Cross( normal, Vector3.Up );
			if ( t1.Length > t2.Length )
			{
				tangent = t1;
			}
			else
			{
				tangent = t2;
			}
			binormal = Vector3.Cross( normal, tangent ).Normal;
		}

	}
}
