using System.Runtime.CompilerServices;

namespace BattleQuest.Application.Tests.TestHelpers;

/// <summary>
/// Helper para converter coleções síncronas em IAsyncEnumerable para testes.
/// </summary>
public static class AsyncEnumerableHelper
{
	/// <summary>
	/// Converte uma coleção síncrona de strings em IAsyncEnumerable.
	/// </summary>
	public static async IAsyncEnumerable<string> ToAsyncLines(
		IEnumerable<string> lines,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		foreach (var l in lines)
		{
			ct.ThrowIfCancellationRequested();
			yield return l;
			await Task.Yield();
		}
	}
}
