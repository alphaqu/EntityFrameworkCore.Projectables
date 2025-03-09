using System.Diagnostics.CodeAnalysis;

namespace EntityFrameworkCore.Projectables
{
    public static class Projectable<T>
    {
        /// <summary>
        /// Creates a shallow copy of the fields, so that efcore has an easier time analysing fields.
        /// Often used for json-based models where you take your json model, and show efcore all of the fields in the select query.
        /// </summary>
        public static T Spread(T value) => value;

        /// <summary>
        /// Inherits the code from the base method, and appends the additional one
        /// </summary>
        public static T Join<TAdd0>(
            TAdd0 v0
        )
        {
            var init = (T)Activator.CreateInstance(typeof(T))!;
            foreach (var info in typeof(TAdd0).GetProperties())
            {
                info.SetValue(init, info.GetValue(v0));
            }
            
            return init;
        }
        
        public static T Join<TAdd0, TAdd1>(
            TAdd0 v0,
            TAdd1 v1
        )
        {
            var init = (T)Activator.CreateInstance(typeof(T))!;
            foreach (var info in typeof(TAdd0).GetProperties())
            {
                info.SetValue(init, info.GetValue(v0));
            }

            foreach (var info in typeof(TAdd1).GetProperties())
            {
                info.SetValue(init, info.GetValue(v1));
            }

            return init;
        }

    }
}