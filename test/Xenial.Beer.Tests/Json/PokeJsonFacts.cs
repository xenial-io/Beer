using System;

using Shouldly;

using Xenial.Delicious.Beer.Json;

using static Xenial.Tasty;

namespace Xenial.Delicious.Beer.Tests.Json
{
    public static class PokeJsonFacts
    {
        public static void PokeJsonTests() => Describe(nameof(PokeJson), () =>
        {
            It("should add simple setting", () =>
            {
                var json = "{}";
                PokeJson.AddOrUpdateJsonValue(json, "MyKey", true).ShouldBe(@"{
  ""MyKey"": true
}");
            });

            It("should add simple value with section", () =>
            {
                var json = "{}";
                PokeJson.AddOrUpdateJsonValue(json, "MySection:MyValue", true).ShouldBe(@"{
  ""MySection"": {
    ""MyValue"": true
  }
}");
            });

            It("should add simple value with complex section", () =>
            {
                var json = "{}";
                PokeJson.AddOrUpdateJsonValue(json, "MySection:Nested:MyValue", true).ShouldBe(@"{
  ""MySection"": {
    ""Nested"": {
      ""MyValue"": true
    }
  }
}");
            });

            It("should add anon object value with complex section", () =>
            {
                var json = "{}";
                PokeJson.AddOrUpdateJsonValue(json, "MySection:Nested:Nested:Nested", new { }).ShouldBe(@"{
  ""MySection"": {
    ""Nested"": {
      ""Nested"": {
        ""Nested"": {}
      }
    }
  }
}");
            });

            It("should add replace object value with null", () =>
            {
                var json = @"{
  ""MySection"": {
    ""Nested"": {
      ""Nested"": {
        ""Nested"": {}
      }
    }
  }
}";
                PokeJson.AddOrUpdateJsonValue<object?>(json, "MySection:Nested", null).ShouldBe(@"{
  ""MySection"": {
    ""Nested"": null
  }
}");
            });

            It("anonymous type and deep nesting should produce the same resuld", () =>
            {
                var json1 = PokeJson.AddOrUpdateJsonValue(string.Empty, "MySection:Nested:Val", 1);
                var json2 = PokeJson.AddOrUpdateJsonValue(string.Empty, "MySection", new
                {
                    Nested = new
                    {
                        Val = 1
                    }
                });

                json1.ShouldBe(json2);
            });
        });
    }
}
