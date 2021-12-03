using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
namespace MinimalExample
{
    internal class QuadTree
    {
		int state; //0 is empty, 1 is full, -1 is "complex" ie, children have data

		private QuadTree? node00;
		private QuadTree? node01;
		private QuadTree? node10;
		private QuadTree? node11;
		private int extentsXMin;
		private int extentsXMax;
		private int extentsYMin;
		private int extentsYMax;
		private int midPointX;
		private int midPointY;
		private QuadTree parent;
		public QuadTree( QuadTree parent, int xmin, int xmax, int ymin, int ymax )
		{
			this.parent = parent;
			this.state = 0;
			//Node 00 is top-left
			//Node 01 is bottom-left
			//First bit is for horizontal
			//Second bit is vertical
			node00 = null;
			node01 = null;
			node10 = null;
			node11 = null;

			extentsXMin = xmin;
			extentsXMax = xmax;
			extentsYMin = ymin;
			extentsYMax = ymax;
			midPointX = (extentsXMin + extentsXMax) / 2;
			midPointY = (extentsYMin + extentsYMax) / 2;
		}

		public QuadTree( QuadTree parent, int xmin, int xmax, int ymin, int ymax, bool isAllTrue ) : this( parent, xmin, xmax, ymin, ymax )
		{
			this.state = isAllTrue ? 1 : 0;
		}

		public QuadTree( QuadTree parent, int xmin, int xmax, int ymin, int ymax, bool[,] data ) : this( parent, xmin, xmax, ymin, ymax )
		{
			(bool, bool) aisibResult = allIsSameInBounds( extentsXMin, extentsXMax, extentsYMin, extentsYMax, data );
			if ( aisibResult.Item1 )
			{
				this.state = aisibResult.Item2 ? 1 : 0;
			}
			else
			{
				this.state = -1;
				this.node00 = new QuadTree( this, extentsXMin, midPointX, extentsYMin, midPointY, data );
				this.node01 = new QuadTree( this, extentsXMin, midPointX, midPointY + 1, extentsYMax, data );
				this.node10 = new QuadTree( this, midPointX + 1, extentsXMax, extentsYMin, midPointY, data );
				this.node11 = new QuadTree( this, midPointX + 1, extentsXMax, midPointY + 1, extentsYMax, data );
			}
		}

		private void ensureInBounds( int x, int y )
		{
			if ( !isInNodeBounds( x, y ) )
			{
				throw new ArgumentOutOfRangeException();
			}
		}

		public void buildFromRandom(Func<int, int, bool> generator)
		{
			if ( extentsXMax == extentsXMin )
			{
				//Console.WriteLine($"Hit a 1x1, result {generator(extentsXMin, extentsYMin)}");
				this.state = generator( extentsXMin, extentsYMin ) ? 1 : 0;
			}
			else
			{
				//Console.WriteLine("Recursing");
				this.state = -1;
				node00 = new QuadTree( this, extentsXMin, midPointX, extentsYMin, midPointY );
				node01 = new QuadTree( this, extentsXMin, midPointX, midPointY + 1, extentsYMax );
				node10 = new QuadTree( this, midPointX + 1, extentsXMax, extentsYMin, midPointY );
				node11 = new QuadTree( this, midPointX + 1, extentsXMax, midPointY + 1, extentsYMax );
				node00.buildFromRandom( generator );
				node01.buildFromRandom( generator );
				node10.buildFromRandom( generator );
				node11.buildFromRandom( generator );
				//Console.WriteLine($"{node00.state} {node01.state} {node10.state} {node11.state}");
				int childState = node00.state;
				if ( childState != -1 && node00.state == node01.state && node01.state == node10.state && node10.state == node11.state )
				{ //All children are all solid and all the same color
				  //Console.WriteLine("merging");
					node00 = null;
					node01 = null;
					node10 = null;
					node11 = null;
					this.state = childState;
				}
			}
		}

		bool isInNodeBounds( int x, int y )
		{
			return (extentsXMin <= x && x <= extentsXMax) && (extentsYMin <= y && y <= extentsYMax);
		}

		private QuadTree getChildAccordingToPosition( int x, int y )
		{
			ensureInBounds( x, y );
			if ( state != -1 )
			{ //I'm not sure if this needs to be here.
				return null;
			}
			if ( x <= midPointX )
			{
				if ( y <= midPointY )
				{
					return node00;
				}
				else
				{
					return node01;
				}
			}
			else
			{
				if ( y <= midPointY )
				{
					return node10;
				}
				else
				{
					return node11;
				}
			}
		}

		public bool getValue( int x, int y )
		{
			ensureInBounds( x, y );
			if ( this.state != -1 )
			{
				return this.state == 1;
			}
			else
			{
				//Note, if it is on the midpoint, it belongs to the lower value
				//Ignore the possible null reference warning, as if state == -1 then
				//There must be children nodes.
				return getChildAccordingToPosition( x, y ).getValue( x, y );
			}
		}

		//First bool is true iff they are all the same, if they are
		//then the 2nd is the value of all the elements
		private static (bool, bool) allIsSameInBounds( int xmin, int xmax, int ymin, int ymax, bool[,] data )
		{
			bool firstValue = data[xmin, ymin];
			for ( int x = xmin; x <= xmax; x++ )
			{
				for ( int y = ymin; y <= ymax; y++ )
				{
					if ( data[x, y] != firstValue )
					{
						return (false, false);
					}
				}
			}
			return (true, firstValue);
		}

		public void setPosition( int x, int y, bool value )
		{
			ensureInBounds( x, y );
			if ( state != -1 )
			{ //The current node is completely solid
				if ( value == (state == 1) )
				{ //We are setting this node to its current color, do nothing
					return;
				}
				else
				{ //Otherwise, we need to do work
					if ( extentsXMin == extentsXMax )
					{ //We are in a 1x1 node
						state = value ? 1 : 0;
						//This is the only case, where we actually changed a value, which we need to repair the integrity.
						fixIntegrity();
					}
					else
					{
						bool filled = state == 1;
						state = -1;
						this.node00 = new QuadTree( this, extentsXMin, midPointX, extentsYMin, midPointY, filled );
						this.node01 = new QuadTree( this, extentsXMin, midPointX, midPointY + 1, extentsYMax, filled );
						this.node10 = new QuadTree( this, midPointX + 1, extentsXMax, extentsYMin, midPointY, filled );
						this.node11 = new QuadTree( this, midPointX + 1, extentsXMax, midPointY + 1, extentsYMax, filled );
						getChildAccordingToPosition( x, y ).setPosition( x, y, value );
					}
				}
			}
			else
			{ //The current node is not solid
				getChildAccordingToPosition( x, y ).setPosition( x, y, value );
			}
		}

		private void fixIntegrity()
		{
			if ( state == -1 )
			{ //We are in a mixed-color node
			  //Agains, since state == -1, nodes are not null.
				int childState = node00.state;
				if ( childState != -1 && node00.state == node01.state && node01.state == node10.state && node10.state == node11.state )
				{ //All children are all solid and all the same color
					node00 = null;
					node01 = null;
					node10 = null;
					node11 = null;
					this.state = childState;
					if ( this.parent != null )
					{ // We are the root.
						this.parent.fixIntegrity();
					}
				}
			}
			if ( this.parent != null )
			{ // We are the root.
				this.parent.fixIntegrity();
			}
		}

		public void printExtentsAndData(int recursiveLevel)
        {
			Log.Info( $"{recursiveLevel} X: [{extentsXMin}, {extentsXMax}], Y: [{extentsYMin}, {extentsYMax}], {this.state}" );

			//Console.WriteLine($"X: [{extentsXMin}, {extentsXMax}], Y: [{extentsYMin}, {extentsYMax}], {this.state}");
            if(this.state == -1)
            {
                node00.printExtentsAndData(recursiveLevel + 1);
                node01.printExtentsAndData(recursiveLevel + 1);
                node10.printExtentsAndData(recursiveLevel + 1);
                node11.printExtentsAndData(recursiveLevel + 1);
            }
        }
		
		public void display(Vector3 startPos, float scale)
		{
			Vector3 extentsMin = new Vector3( extentsXMin, extentsYMin, 0 ) * scale;
			Vector3 extentsMax = new Vector3( extentsXMax, extentsYMax, 0 ) * scale;
			if(state == 1)
			{
				DebugOverlay.Box( startPos + extentsMin, startPos + extentsMax, Color.Green, 1 );
			}
			if( this.state == -1)
			{
				node00.display( startPos, scale );
				node01.display( startPos, scale );
				node10.display( startPos, scale );
				node11.display( startPos, scale );
			}
		}
    }
}
