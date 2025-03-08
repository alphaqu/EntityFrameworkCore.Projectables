namespace EntityFrameworkCore.Projectables
{
    public static class Projectable
    {
        /// <summary>
        /// Inherits the code from the base method, and appends the additional one
        /// </summary>
        public static T Extend<T, TBase>(TBase @base, T additional)
            where T : TBase
        {

            foreach (var info in typeof(TBase).GetProperties())
            {
                var value = info.GetValue(@base);
                info.SetValue(additional, value);
            }

            return additional;
        }
    }
}