using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace Terrain.GeoMipMap
{
	class Indices : GameComponent
	{
		Terrain terrain;
		int size;
		int levels;
		List<int>[] indexChain;
		Dictionary<Point, List<int>>[] stitching;

		public Indices(Game game, Terrain terrain) : base(game)
		{
			this.terrain = terrain;
		}

		public int[] GetIndices(int level, int[] neighborLevels)
		{
			List<int> indices = new List<int>(indexChain[level]);

			if (level < levels - 1)
			{
				for (int i = 0; i < neighborLevels.Length; i++)
					if (level < neighborLevels[i])
						indices.AddRange(stitching[i][new Point(level, neighborLevels[i])]);
					else
						indices.AddRange(stitching[i][new Point(level, level)]);
			}

			return indices.ToArray();
		}

		public override void Initialize()
		{
			base.Initialize();

			size = terrain.BlockSize;
			levels = terrain.Levels;

			InitializeIndices();
			InitializeStitching();
		}

		private void InitializeIndices()
		{
			indexChain = new List<int>[levels];

			for (int level = 0; level < levels; level++)
			{
				indexChain[level] = new List<int>();
				int delta = (int)Math.Pow(2, level);

				int offset = (level < levels - 1) ? delta : 0;

				for (int i = offset; i < size - 1 - offset; i += delta)
					for (int j = offset; j < size - 1 - offset; j += delta)
					{
						if ((i % (2 * delta)) + (j % (2 * delta)) != delta)
						{
							indexChain[level].Add(terrain.GetIndex(i, j));
							indexChain[level].Add(terrain.GetIndex(i, j + delta));
							indexChain[level].Add(terrain.GetIndex(i + delta, j + delta));

							indexChain[level].Add(terrain.GetIndex(i + delta, j + delta));
							indexChain[level].Add(terrain.GetIndex(i + delta, j));
							indexChain[level].Add(terrain.GetIndex(i, j));
						}
						else
						{
							indexChain[level].Add(terrain.GetIndex(i, j + delta));
							indexChain[level].Add(terrain.GetIndex(i + delta, j + delta));
							indexChain[level].Add(terrain.GetIndex(i + delta, j));

							indexChain[level].Add(terrain.GetIndex(i + delta, j));
							indexChain[level].Add(terrain.GetIndex(i, j));
							indexChain[level].Add(terrain.GetIndex(i, j + delta));
						}
					}
			}
		}

		private void InitializeStitching()
		{
			stitching = new Dictionary<Point, List<int>>[4];

			for (int i = 0; i < 4; i++)
			{
				stitching[i] = new Dictionary<Point, List<int>>();

				for (int from = 0; from < levels - 1; from++)
					for (int to = from; to < levels; to++)
						stitching[i].Add(new Point(from, to), GetStitching(i, from, to));
			}
		}

		private List<int> GetStitching(int location, int fromLevel, int toLevel)
		{
			var stitching = new List<int>();

			int deltaFromValue = (int)Math.Pow(2, fromLevel);
			int deltaToValue = (int)Math.Pow(2, toLevel);

			int maxFrom = size - 1 - deltaFromValue;
			int maxTo = size - 1;

			Point deltaFrom = (location == 0 || location == 2) ? new Point(deltaFromValue, 0) : new Point(0, deltaFromValue);
			Point deltaTo = (location == 0 || location == 2) ? new Point(deltaToValue, 0) : new Point(0, deltaToValue);

			if (location == 2 || location == 3)
			{
				deltaFrom = new Point(-deltaFrom.X, -deltaFrom.Y);
				deltaTo = new Point(-deltaTo.X, -deltaTo.Y);
			}

			Point halfDeltaTo = new Point(deltaTo.X / 2, deltaTo.Y / 2);
			
			Point firstFrom, firstTo, lastFrom, lastTo;

			firstFrom.X = (location == 1 || location == 2) ? maxFrom : deltaFromValue;
			firstFrom.Y = (location == 0 || location == 3) ? maxFrom : deltaFromValue;

			firstTo.X = (location == 1 || location == 2) ? maxTo : 0;
			firstTo.Y = (location == 0 || location == 3) ? maxTo : 0;

			lastFrom.X = (location == 3 || location == 2) ? deltaFromValue : maxFrom;
			lastFrom.Y = (location == 2 || location == 3) ? deltaFromValue : maxFrom;

			lastTo.X = (location == 3 || location == 2) ? 0 : maxTo;
			lastTo.Y = (location == 2 || location == 3) ? 0 : maxTo;

			Point from = firstFrom;
			Point nextFrom = GetNext(from, deltaFrom);

			Point to = firstTo;
			Point nextTo = GetNext(to, deltaTo);

			if (firstFrom == lastFrom)
			{
				while (to != lastTo)
				{
					AddTriangle(stitching, from, to, nextTo);
					to = nextTo;
					nextTo = GetNext(to, deltaTo);
				}

				return stitching;
			}

			if (from.X == nextTo.X || from.Y == nextTo.Y)
				AddTriangle(stitching, from, to, nextTo);

			int count = 0;

			while (from != lastFrom)
			{
				Point previousFrom = GetPrevious(from, deltaFrom);
				Point middle = GetNext(to, halfDeltaTo);

				bool end = false;

				while (from.X != nextTo.X && from.Y != nextTo.Y && !end)
				{
					if (fromLevel == toLevel)
					{
						if (count % 2 == 0)
						{
							AddTriangle(stitching, to, nextTo, nextFrom);
							AddTriangle(stitching, nextFrom, from, to);
						}
						else
						{
							AddTriangle(stitching, from, to, nextTo);
							AddTriangle(stitching, nextTo, nextFrom, from);
						}						
					}
					else
					{
						if (from != firstFrom && IsBetween(from, to, middle))
							AddTriangle(stitching, from, previousFrom, to);

						if (from.X == middle.X || from.Y == middle.Y)
							AddTriangle(stitching, from, to, nextTo);

						if (from != lastFrom && IsBetween(from, nextTo, middle))
							AddTriangle(stitching, from, nextTo, nextFrom);
					}

					if (from == lastFrom)
						end = true;
					else
					{
						previousFrom = from;
						from = nextFrom;
						nextFrom = GetNext(from, deltaFrom);
					}
				}

				to = nextTo;
				nextTo = GetNext(nextTo, deltaTo);

				count++;
			}

			if (from.X == to.X || from.Y == to.Y)
				AddTriangle(stitching, from, to, nextTo);

			return stitching;
		}

		private bool IsBetween(Point p, Point a, Point b)
		{
			if (a.X == b.X)
				return IsBetween(p.Y, a.Y, b.Y);
			else
				return IsBetween(p.X, a.X, b.X);
		}

		private bool IsBetween(int i, int a, int b)
		{
			return ((i >= a && i <= b) || (i <= a && i >= b));
		}

		private Point GetNext(Point p, Point delta)
		{
			return new Point(p.X + delta.X, p.Y + delta.Y);
		}

		private Point GetPrevious(Point p, Point delta)
		{
			return new Point(p.X - delta.X, p.Y - delta.Y);
		}

		private void AddTriangle(List<int> indices, Point a, Point b, Point c)
		{
			if ((a.X < b.X && c.Y < a.Y) || (a.X > b.X && c.Y > a.Y) || (a.Y < b.Y && c.X > a.X) || (a.Y > b.Y && c.X < a.X))
			{
				indices.Add(terrain.GetIndex(a));
				indices.Add(terrain.GetIndex(b));
				indices.Add(terrain.GetIndex(c));
			}
			else
			{
				indices.Add(terrain.GetIndex(a));
				indices.Add(terrain.GetIndex(c));
				indices.Add(terrain.GetIndex(b));
			}
		}
	}
}
