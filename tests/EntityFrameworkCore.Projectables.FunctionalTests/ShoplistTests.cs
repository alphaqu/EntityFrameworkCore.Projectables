using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityFrameworkCore.Projectables.FunctionalTests.Helpers;
using Microsoft.EntityFrameworkCore;
using VerifyXunit;
using Xunit;

namespace EntityFrameworkCore.Projectables.FunctionalTests
{
    [UsesVerify]
    public class ShoplistTests
    {

        public class AnimalModel
        {
            public int Id { get; set; }
            public double AverageLifespan { get; set; }
            public int Age { get; set; }
        }
        public class CatModel : AnimalModel
        {
            public int MentalAge { get; set; }
            public double MentalLifeProgression { get; set; }
        }
        public abstract class Animal
        {
            public int Id { get; set; }
            public double AverageLifespan { get; set; }
            public int Age { get; set; }

            [Projectable]
            public AnimalModel ToModel() => new AnimalModel {
                Id = Id, 
                AverageLifespan = AverageLifespan * 0.5, 
                Age = Age
            };

        }

        public class Cat : Animal
        {
            public int MentalAge { get; set; }

            [Projectable]
            public double MentalLifeProgression => MentalAge / AverageLifespan;
            
            [Projectable]
            public new CatModel ToModel() => Projectable.Join(base.ToModel(), new CatModel {
                MentalAge = MentalAge,
                MentalLifeProgression = MentalLifeProgression
            });
        }


        [Fact]
        public Task Run()
        {
            using var dbContext = new SampleDbContext<Cat>();

            var query = dbContext
                .Set<Cat>()
                .Select(
                    x => x.ToModel()
                );

            return Verifier.Verify(query.ToQueryString());
        }
    }
}