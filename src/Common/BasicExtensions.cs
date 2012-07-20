﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Common
{
	internal static class BasicExtensions
	{
		internal static bool IsIn<T>(this T item, params T[] list)
		{
			if (list == null || list.Length == 0)
				return false;
			return list.Any(x => EqualityComparer<T>.Default.Equals(x, item));
		}

        internal static Action Chain(this IEnumerable<Action> actions)
		{
			return () =>
			{
				foreach (var action in actions)
					action();
			};
		}
	}

}
