using System.Diagnostics.CodeAnalysis;

namespace ThePlague.Model.Core.Text
{
    public class Utf8OrdinalIgnoreCaseStringComparer : BaseUtf8StringComparer
    {
        public static Utf8OrdinalIgnoreCaseStringComparer Instance => new();

        public override int Compare(Utf8String x, Utf8String y)
            => SimpleCaseFolding.CompareUsingSimpleCaseFolding(x, y);

        public override bool Equals(Utf8String x, Utf8String y)
            => SimpleCaseFolding.CompareUsingSimpleCaseFolding(x, y) == 0;

        public override int GetHashCode([DisallowNull] Utf8String obj)
            => SimpleCaseFolding.GetHashCode(obj);
    }
}
