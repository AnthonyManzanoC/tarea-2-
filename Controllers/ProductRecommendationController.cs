using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using CoronelExpress.Models;
using CoronelExpress.Data;
using System;


namespace CoronelExpress.Controllers
{
    public class ProductRecommendationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MLContext _mlContext;
        private ITransformer _mlModel;
        private PredictionEngine<ProductMLData, ProductMLPrediction> _predEngine;

        
            public ProductRecommendationController(ApplicationDbContext context)
            {
                _context = context;
                _mlContext = new MLContext();
                TrainModel();
            }

            // Se entrena un modelo de clasificación binaria para predecir si un producto es recomendado.
            private void TrainModel()
            {
                // Datos de entrenamiento de ejemplo.
                var trainingData = new List<ProductMLData>
            {
               new ProductMLData { DiscountPercentage = 30, Price = 100, Stock = 50, IsOnOffer = 1f, Label = true },
    new ProductMLData { DiscountPercentage = 10, Price = 200, Stock = 20, IsOnOffer = 1f, Label = false },
    new ProductMLData { DiscountPercentage = 25, Price = 150, Stock = 30, IsOnOffer = 1f, Label = true },
    new ProductMLData { DiscountPercentage = 0,  Price = 50,  Stock = 100, IsOnOffer = 0f, Label = false },
    
    // Datos adicionales para una experiencia VIP más impactante
    new ProductMLData { DiscountPercentage = 40, Price = 80,  Stock = 150, IsOnOffer = 1f, Label = true },   // Alta oferta y precio reducido, ideal para VIP.
    new ProductMLData { DiscountPercentage = 15, Price = 250, Stock = 10,  IsOnOffer = 1f, Label = false },  // Oferta moderada pero producto caro y escaso.
    new ProductMLData { DiscountPercentage = 50, Price = 120, Stock = 40,  IsOnOffer = 1f, Label = true },   // Descuento muy alto, perfecto para destacar.
    new ProductMLData { DiscountPercentage = 5,  Price = 300, Stock = 5,   IsOnOffer = 1f, Label = false },  // Oferta insignificante en un producto premium.
    new ProductMLData { DiscountPercentage = 20, Price = 180, Stock = 80,  IsOnOffer = 1f, Label = true },   // Oferta balanceada en producto de calidad.
    new ProductMLData { DiscountPercentage = 0,  Price = 60,  Stock = 200, IsOnOffer = 0f, Label = false },  // Producto sin oferta, alta disponibilidad.
    new ProductMLData { DiscountPercentage = 35, Price = 90,  Stock = 70,  IsOnOffer = 1f, Label = true },   // Oferta significativa, precio accesible.
    new ProductMLData { DiscountPercentage = 12, Price = 220, Stock = 25,  IsOnOffer = 1f, Label = false },  // Oferta baja en producto de gama alta.
    new ProductMLData { DiscountPercentage = 28, Price = 110, Stock = 55,  IsOnOffer = 1f, Label = true },   // Buen balance entre descuento y precio.
    new ProductMLData { DiscountPercentage = 8,  Price = 190, Stock = 40,  IsOnOffer = 1f, Label = false },  // Oferta mínima en producto costoso.
    // Nuevos datos inteligentes: No recomendar si Stock es 0, junto a otras condiciones VIP
    new ProductMLData { DiscountPercentage = 45, Price = 95,  Stock = 0,   IsOnOffer = 1f, Label = false }, // Stock 0, no recomendar.
    new ProductMLData { DiscountPercentage = 50, Price = 100, Stock = 0,   IsOnOffer = 1f, Label = false }, // Stock 0, no recomendar.
    new ProductMLData { DiscountPercentage = 60, Price = 85,  Stock = 120, IsOnOffer = 1f, Label = true  }, // Descuento altísimo, precio bajo, stock alto.
    new ProductMLData { DiscountPercentage = 10, Price = 130, Stock = 0,   IsOnOffer = 1f, Label = false }, // Stock 0, no importar oferta.
    new ProductMLData { DiscountPercentage = 32, Price = 115, Stock = 60,  IsOnOffer = 1f, Label = true  }, // Oferta atractiva, stock suficiente.
    new ProductMLData { DiscountPercentage = 5,  Price = 350, Stock = 15,  IsOnOffer = 1f, Label = false }, // Producto premium, oferta mínima y stock bajo.
    new ProductMLData { DiscountPercentage = 50, Price = 140, Stock = 30,  IsOnOffer = 1f, Label = true  },// Oferta muy buena, precio medio, stock moderado.
              // Ejemplos adicionales para una toma de decisión ultra inteligente
    new ProductMLData { DiscountPercentage = 55, Price = 70,  Stock = 200, IsOnOffer = 1f, Label = true  },  // Oferta excepcional: descuento altísimo, precio muy bajo y stock abundante.
    new ProductMLData { DiscountPercentage = 20, Price = 90,  Stock = 150, IsOnOffer = 1f, Label = true  },  // Oferta interesante con buen stock.
    new ProductMLData { DiscountPercentage = 15, Price = 110, Stock = 0,   IsOnOffer = 1f, Label = false }, // Stock 0, no se recomienda pese a la oferta.
    new ProductMLData { DiscountPercentage = 0,  Price = 100, Stock = 80,  IsOnOffer = 0f, Label = false }, // Sin oferta, no se recomienda.
    new ProductMLData { DiscountPercentage = 35, Price = 95,  Stock = 30,  IsOnOffer = 1f, Label = true  },  // Oferta notable, aunque stock moderado.
    new ProductMLData { DiscountPercentage = 22, Price = 160, Stock = 90,  IsOnOffer = 1f, Label = false }, // Oferta moderada en producto con precio elevado.
    new ProductMLData { DiscountPercentage = 70, Price = 50,  Stock = 300, IsOnOffer = 1f, Label = true  },  // Descuento extremo, producto súper atractivo para VIP.
    new ProductMLData { DiscountPercentage = 12, Price = 210, Stock = 70,  IsOnOffer = 1f, Label = false }, // Oferta leve en producto de precio alto.
    new ProductMLData { DiscountPercentage = 48, Price = 130, Stock = 45,  IsOnOffer = 1f, Label = true  },  // Oferta muy atractiva con stock aceptable.
    new ProductMLData { DiscountPercentage = 0,  Price = 80,  Stock = 25,  IsOnOffer = 0f, Label = false },  // Sin oferta, no se recomienda.
    
    // Ejemplos extremos para validar límites y robustez
    new ProductMLData { DiscountPercentage = 80, Price = 40,  Stock = 500, IsOnOffer = 1f, Label = true  },  // Ultra descuento, precio ínfimo, stock masivo.
    new ProductMLData { DiscountPercentage = 90, Price = 45,  Stock = 400, IsOnOffer = 1f, Label = true  },  // Descuento casi gratis, experiencia VIP extrema.
    new ProductMLData { DiscountPercentage = 0,  Price = 1000, Stock = 5,  IsOnOffer = 0f, Label = false }, // Producto premium sin oferta, stock bajo.
    new ProductMLData { DiscountPercentage = 65, Price = 200, Stock = 50,  IsOnOffer = 1f, Label = true  },  // Alta eficiencia: descuento alto y precio moderado.
    new ProductMLData { DiscountPercentage = 20, Price = 150, Stock = 0,  IsOnOffer = 1f, Label = false },  // Stock nulo, se descarta siempre.
    new ProductMLData { DiscountPercentage = 55, Price = 80,  Stock = 100, IsOnOffer = 1f, Label = true  },  // Fuerte oferta con stock balanceado.
    new ProductMLData { DiscountPercentage = 35, Price = 95,  Stock = 30,  IsOnOffer = 1f, Label = true  },  // Consistente propuesta de valor.
    new ProductMLData { DiscountPercentage = 15, Price = 220, Stock = 80,  IsOnOffer = 1f, Label = false }, // Oferta baja en producto costoso.
    new ProductMLData { DiscountPercentage = 50, Price = 120, Stock = 70,  IsOnOffer = 1f, Label = true  }   // Oferta sólida, stock decente, precio balanceado.
};

            var trainingDataView = _mlContext.Data.LoadFromEnumerable(trainingData);

                // Se concatena las columnas de características, todas en tipo float.
                var pipeline = _mlContext.Transforms.Concatenate("Features",
                                        nameof(ProductMLData.DiscountPercentage),
                                        nameof(ProductMLData.Price),
                                        nameof(ProductMLData.Stock),
                                        nameof(ProductMLData.IsOnOffer))
                    .Append(_mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression());

                _mlModel = pipeline.Fit(trainingDataView);
                _predEngine = _mlContext.Model.CreatePredictionEngine<ProductMLData, ProductMLPrediction>(_mlModel);
            }

            // GET: /ProductRecommendation
            // Muestra una vista inicial con un botón para ver recomendaciones.
            public IActionResult Index()
            {
                return View();
            }
        // GET: /ProductRecommendation/Details/7
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Busca el producto en la base de datos
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // Opcional: Aquí puedes agregar lógica adicional usando ML
            // Por ejemplo, ejecutar una predicción o ajustar datos específicos

            return View(product);
        }

        // GET: /ProductRecommendation/Recommend
        // Obtiene la lista de productos, predice mediante el modelo ML y devuelve los recomendados.
        public async Task<IActionResult> Recommend()
            {
                // Cargar todos los productos desde la base de datos.
                var products = await _context.Products.ToListAsync();
                var recommendedProducts = new List<Product>();

                foreach (var product in products)
                {
                    // Convertir la propiedad booleana a float (1 para true, 0 para false).
                    var input = new ProductMLData
                    {
                        DiscountPercentage = (float)product.DiscountPercentage,
                        Price = (float)product.Price,
                        Stock = product.Stock,
                        IsOnOffer = product.IsOnOffer ? 1f : 0f
                    };

                    var prediction = _predEngine.Predict(input);

                    // Si el modelo predice "true", se agrega el producto a la lista de recomendados.
                    if (prediction.PredictedLabel)
                    {
                        recommendedProducts.Add(product);
                    }
                }

                // Retorna la vista "Recommend" con los productos recomendados.
                return View("Index", recommendedProducts);
            }
        }



        // Clases para ML.NET
        public class ProductMLData
    {
        public float DiscountPercentage { get; set; }
        public float Price { get; set; }
        public float Stock { get; set; }
        // Convertir IsOnOffer a float: 1 si es true, 0 si es false.
        public float IsOnOffer { get; set; }
        // Label: true si se recomienda, false de lo contrario.
        public bool Label { get; set; }
    }


    public class ProductMLPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool PredictedLabel { get; set; }
        public float Probability { get; set; }
        public float Score { get; set; }
    }
}