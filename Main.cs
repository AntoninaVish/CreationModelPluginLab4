using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices; //добавляем вручную


namespace CreationModelPluginLab4

{
    [Transaction(TransactionMode.Manual)]

    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            Document doc = commandData.Application.ActiveUIDocument.Document; //получаем доступ к документу Revit(ссылка на документ)

            Level level1, level2;
            TakeLevels(doc, out level1, out level2);
            CreateWalls(doc, level1, level2);

            return Result.Succeeded;

        }
        private static void CreateWalls(Document doc, Level level1, Level level2)
        {
            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);//задаем размеры будущего дома (ширина)
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);// парраметер глубина
            double dx = width / 2; //получаем набор точек(делим ширину и глубину пополам записываем в переменные dx и dy
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();//коллекцию с точками
            points.Add(new XYZ(-dx, -dy, 0)); // получаем за счет ворлирования смещения в положительную или отрицательную сторону
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();//Массив в котором добавляем созданные стены (пустой массив)

            Transaction transaction = new Transaction(doc, "Построение стен");  //транзакция внутри которой цикл который будет создавать стены

            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);//для каждой стены предворительно создаем отрезок при помощи статического метода CreateBound 
                                                                       // в качестве аргумента передаем две точки из коллекции points  с индексо i  и с интексом i+1

                Wall wall = Wall.Create(doc, line, level1.Id, false);
                //стоим стену для создания экземпляра системного симейства используется статический метод Create
                walls.Add(wall); //когда создали стену добавляем ее в массив стен
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);//привязка верха стены к уровню 2

            }
            CreateDoor(doc, level1, walls[0]);
            //добавляем двери, передаем ссылку на документ, передаем значение уровня, передаем стену в которую добавляем дверь
            //берем первую из расчета массива
            CreateWindow(doc, level1, walls[1]);
            CreateWindow(doc, level1, walls[2]);
            CreateWindow(doc, level1, walls[3]);

            AddRoof(doc, level2, walls, width, depth);//Добавляем крышу, передаем документ, этаж и все стены, ширину и грубину

            transaction.Commit();
        }

        private static void AddRoof(Document doc, Level level2, List<Wall> walls, double width, double depth)// метод для добавления крышу
        {
            RoofType roofType = new FilteredElementCollector(doc)
                 .OfClass(typeof(RoofType))
                 .OfType<RoofType>()
                 .Where(x => x.Name.Equals("Типовой - 400мм"))
                 .Where(x => x.FamilyName.Equals("Базовая крыша"))
                 .FirstOrDefault();


            ////1 СПОСОБ ПО ВИДЕОУРОКУ
            //double wallWidth = walls[0].Width;
            //double dt = wallWidth / 2; //получаем смещение крыши 
            //List<XYZ> points = new List<XYZ>(); //массив точек

            //points.Add(new XYZ(-dt, -dt, 0)); // получаем за счет ворлирования смещения в положительную или отрицательную сторону
            //points.Add(new XYZ(dt, -dt, 0));
            //points.Add(new XYZ(dt, dt, 0));
            //points.Add(new XYZ(-dt, dt, 0));
            //points.Add(new XYZ(-dt, -dt, 0));

            //Application application = doc.Application;
            //CurveArray footprint = application.Create.NewCurveArray();//footprint это отпечаток границы дома по которой автоматически
            ////будет построена крыша 

            //for (int i = 0; i < 4; i++) //перебераем все стены
            //{
            //    LocationCurve curve = walls[i].Location as LocationCurve; //получаем кривую
            //    XYZ p1 = curve.Curve.GetEndPoint(0); //добавляем линию со смещением
            //    XYZ p2 = curve.Curve.GetEndPoint(1); //добавляем линию со смещением
            //    Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]);
            //    //получаем линию созданную при помощи метода CreateBound (аргументами будут р1+points (соответствующее смещение) и р2+points(смещение)

            //    footprint.Append(line);//добавляем созданную линию 
            //}

            //ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray(); //Создаем пустой массив (для крыши)
            //FootPrintRoof footprintRoof = doc.Create.NewFootPrintRoof(footprint, level2, roofType, out footPrintToModelCurveMapping);

            ////ModelCurveArrArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator(); //наклон крыши
            ////iterator.Reset();
            ////while (iterator.MoveNext())
            ////{
            ////    ModelCurve modelCurve = iterator.Current as ModelCurve;
            ////    footprintRoof.set_DefinesSlope(modelCurve, true);
            ////    footprintRoof.set_SlopeAngle(modelCurve, 0.5);
            ////}
            //foreach (ModelCurve m in footPrintToModelCurveMapping)
            //{
            //    footprintRoof.set_DefinesSlope(m, true);
            //    footprintRoof.set_SlopeAngle(m, 0.5);
            //}


            //2 СПОСОБ ПО ДОМАШНЕМУ ЗАДАНИЮ
            View view = new FilteredElementCollector(doc)
               .OfClass(typeof(View))
               .OfType<View>()
               .Where(x => x.Name.Equals("Уровень 1"))
               .FirstOrDefault();

            double wallWight = walls[0].Width; // ширина стены
            double dt = wallWight / 2;

            double extrusionStart = -width / 2 - dt; // смещение
            double extrusionEnd = width / 2 + dt;
            double curveStart = -depth / 2 - dt;
            double curveEnd = +depth / 2 + dt;

            CurveArray curveArray = new CurveArray();  //отпечаток границы дома
            curveArray.Append(Line.CreateBound(new XYZ(0, curveStart, level2.Elevation), new XYZ(0, 0, level2.Elevation + 10)));
            curveArray.Append(Line.CreateBound(new XYZ(0, 0, level2.Elevation + 10), new XYZ(0, curveEnd, level2.Elevation)));

            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20), new XYZ(0, 20, 0), view);
            ExtrusionRoof extrusionRoof = doc.Create.NewExtrusionRoof(curveArray, plane, level2, roofType, extrusionStart, extrusionEnd);
            extrusionRoof.EaveCuts = EaveCutterType.TwoCutSquare;


        }

        private static void CreateWindow(Document doc, Level level1, Wall wall)//метод создания окон
        {
            FamilySymbol winType = new FilteredElementCollector(doc)//находим нужный типаразмер, заносим в переменную FamilySymbol
                 .OfClass(typeof(FamilySymbol))
                 .OfCategory(BuiltInCategory.OST_Windows)
                 .OfType<FamilySymbol>()//фильтрация по типу FamilySymbol
                 .Where(x => x.Name.Equals("0915 x 1830 мм"))
                 .Where(x => x.FamilyName.Equals("Фиксированные"))//семейство к которому добираемся
                 .FirstOrDefault();//если мы хотим получить единичный экземпляр(результатом будет один экземпляр FamilySymbol)

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2; //получаем на основе этих точек среднюю точку

            if (!winType.IsActive)
                winType.Activate();

            var window = doc.Create.NewFamilyInstance(point, winType, wall, level1, StructuralType.NonStructural);
            Parameter silHeingt = window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
            double sh = UnitUtils.ConvertToInternalUnits(900, UnitTypeId.Millimeters);
            silHeingt.Set(sh);

        }

        private static void CreateDoor(Document doc, Level level1, Wall wall)//создаем метод
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)//находим нужный типаразмер, заносим в переменную FamilySymbol
                 .OfClass(typeof(FamilySymbol))
                 .OfCategory(BuiltInCategory.OST_Doors)
                 .OfType<FamilySymbol>()//фильтрация по типу FamilySymbol
                 .Where(x => x.Name.Equals("0915 x 2134 мм"))
                 .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))//семейство к которому добираемся
                 .FirstOrDefault();//если мы хотим получить единичный экземпляр(результатом будет один экземпляр FamilySymbol)

            LocationCurve hostCurve = wall.Location as LocationCurve;
            //определяем точку в которую будем добавлять дверь(чтобы получить границы отрезка нужно преобразовать 
            //LOCATION к правильному типу)результатом будет объект типа LocationCurve
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            //добираемся к точкам левой и правой границе кривой при помощи метода GetEndPoint
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2; //получаем на основе этих точек среднюю точку

            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
            //на объекте doc обращаемся к свойству Create и передаем аргументы

        }
        private static void TakeLevels(Document doc, out Level level1, out Level level2)
        {
            List<Level> listLevel1 = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            level1 = listLevel1
                .Where(x => x.Name.Equals("Уровень 1"))
                .FirstOrDefault();
            level2 = listLevel1
               .Where(x => x.Name.Equals("Уровень 2"))
               .FirstOrDefault();



        }
    }

}







//ЭТА ЧАСТЬ ОБЩАЯ ДЛЯ ВСЕХ ЭТАЖЕЙ(УРОВНЕЙ)
//List<Level> listLevel = new FilteredElementCollector(doc) // фильтр для поиска уровней (именно этажей а не всех элементов)
//     .OfClass(typeof(Level)) //уровни определяются классом Level
//     .OfType<Level>()
//     .ToList();
//    Level level1 = listLevel // фильтр для поиска уровней конкретного уровня
//        .Where(x => x.Name.Equals("Уровнь 1"))//метод расширения LINQ
//        .FirstOrDefault(); //получаем этот уровень 
//    Level level2 = listLevel
//        .Where(x => x.Name.Equals("Уровнь 2"))//метод расширения LINQ
//        .FirstOrDefault(); //получаем этот уровень 


////ФОРМИРОВАНИЕ СПИСКА С КООРДИНАТАМИ ТОЧЕК ЧЕРЕЗ КОТОРЫЕ ДОЛЖНЫ ПРОХОДИТЬ СТЕНЫ
//double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);//задаем размеры будущего дома (ширина)
//double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);// парраметер гдубина
//double dx = width / 2; //получаем набор точек(делим ширину и глубину пополам записываем в переменные dx и dy
//double dy = depth / 2;

//List<XYZ> points = new List<XYZ>();//коллекцию с точками
//points.Add(new XYZ(-dx, -dy, 0)); // получаем за счет ворлирования смещения в положительную или отрицательную сторону
//points.Add(new XYZ(dx, -dy, 0)); 
//points.Add(new XYZ(dx, dy, 0)); 
//points.Add(new XYZ(-dx, dy, 0));
//points.Add(new XYZ(-dx, -dy, 0));

//List<Wall> walls = new List<Wall>();//Массив в котором добавляем созданные стены (пустой массив)

//using (Transaction transaction = new Transaction(doc, "Построение стен"))  //транзакция внутри которой цикл который будет создавать стены
//{
//    transaction.Start();
//    for (int i = 0; i < 4; i++)
//    {
//        Line line = Line.CreateBound(points[i], points[i + 1]);//для каждой стены предворительно создаем отрезок при помощи статического метода CreateBound 
//                                                               // в качестве аргумента передаем две точки из коллекции points  с индексо i  и с интексом i+1

//        Wall wall = Wall.Create(doc, line, level1.Id, false);
//        //стоим стену для создания экземпляра системного симейства используется статический метод Create
//        walls.Add(wall); //когда создали стену добавляем ее в массив стен
//        wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);//привязка верха стены к уровню 2

//    }

//    transaction.Commit();
//}

////ЭТА ЧАСТЬ ДЛЯ ОДНОГО ЭТАЖА(УРОВНЯ)
//Level level1 = new FilteredElementCollector(doc) // фильтр для поиска уровней 
//    .OfClass(typeof(Level)) //уровни определяются классом Level
//    .Where(x => x.Name.Equals("Уровнь 1"))//метод расширения LINQ
//    .OfType<Level>() //преобразование (фильтрация), выступает переменная типа Level
//    .FirstOrDefault(); //получаем этот уровень 



////ЭТО СИСТЕМНЫЕ СИМЕЙСТВА
//var res1 = new FilteredElementCollector(doc) //находим все стены в проекте через фильтр
//    .OfClass(typeof(WallType)) //в качестве аргумента указываем нужный тип
//    //.Cast<Wall>()
//    .OfType<WallType>() //выполняет фильтрацию на основе заданного типа(LINQ фильтрацию на основе WallType)
//    .ToList(); //список параметризированный базовым классом Element
//               // на выходе получим список стен

////ЭТО ЗАГРУЖАЕМЫЕ СИМЕЙСТВА фильтрация по типу FamilyInstance
//var res2 = new FilteredElementCollector(doc) //находим все стены в проекте через фильтр
//   .OfClass(typeof(FamilyInstance)) //в качестве аргумента указываем нужный тип (быстрые Revit фильтры пишем всегда вначале!!!)
//   .OfCategory(BuiltInCategory.OST_Doors) //фильтр по категориям (быстрые Revit фильтры пишем всегда вначале!!!)
//    //.Cast<Wall>() // медленный Revit фильтр

//   .OfType<FamilyInstance>() //выполняет фильтрацию на основе заданного типа
//   .Where(x=>x.Name.Equals("0915 x 2134 мм")) //выполняет фильтр по названию
//   .ToList(); //список параметризированный базовым классом Element
//              // на выходе получим список стен

////БЫСТРЫЕ ФИЛЬТРЫ
//var res3 = new FilteredElementCollector(doc) //находим все стены в проекте через фильтр
//   .WhereElementIsNotElementType()
//   .ToList(); //список параметризированный базовым классом Element
//              // на выходе получим список стен





//return Result.Succeeded;

//}
//}

//Чтобы искать по типу используется фильтр OfClass этот метод расширения относиться именно к Revit
//Метод расширения CAST выполняет преобразования каждого элемента в списке к заданному типу
//ToList();  список параметризированный базовым классом Element
//OfType выполняет фильтрацию на основе заданного типа, т.е он не выполняет преобразования в стену, а отбирает 
// в списке все стены и на выходе получаем список стен.
// Переменная VAR тип которой определяется по коду.
//Чтобы отделить двери от окон добавляем фильт по категории OfCategory, при поиске отделяет одни семейства от других.
// Чтобы отделять типы от экземпляров (напр. Wall от WallType) можно делать фильтр на основе типа OfClass(typeof(FamilyInstance))
//В Revit измерение в фут.
//Свойство LOCATION есть у многих элементов являться может либо точкой либо кривой.
//footprint.Append(curve.Curve);//добавляем кривую в footprint при помощи метода Append