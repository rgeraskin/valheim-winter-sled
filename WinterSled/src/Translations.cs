using System.Collections.Generic;
using Jotunn.Managers;

namespace WinterSled
{
    /// <summary>
    /// Every language the game ships (its localization file's columns). The game's own files leave some
    /// languages and words empty, so nothing here leans on its tokens; Jötunn falls back to English for a
    /// language that is missing a key. Abenaki is left to that fallback: no reliable translation was at hand.
    /// </summary>
    internal static class Translations
    {
        // the order of the strings in each row below
        internal static readonly string[] Keys =
        {
            "piece_wintersled",
            "piece_wintersled_desc",
            "wintersled_ride",
            "wintersled_steer",
            "wintersled_brake",
            "wintersled_push",
            "wintersled_getoff",
            "se_wintersled",
            "se_wintersled_tooltip",
            "wintersled_surface_snow",
            "wintersled_surface_powder",
            "wintersled_surface_ice",
            "wintersled_surface_wood",
            "wintersled_surface_stone",
            "wintersled_surface_ground",
            "wintersled_surface_water",
            "se_wintersled_cart",
            "se_wintersled_cart_tooltip",
            "wintersled_surface_frozen",
        };

        internal static readonly Dictionary<string, string[]> Rows = new Dictionary<string, string[]>
        {
            ["English"] = new[]
            {
                "Winter Sled",
                "A cargo sled on iron-shod runners. It glides over snow and ice but drags heavily over bare ground. Sit in it to ride down a snowy slope.",
                "Ride", "steer", "brake", "push off", "get off",
                "Hauling a sled", "Runners glide on snow and ice; bare ground slows you down.",
                "Snow", "Deep snow", "Ice", "Wood", "Stone", "Ground", "Water",
                "Wheels in snow", "The cart's wheels sink in snow and slow you down. A sled would glide here.",
                "Frozen ground",
            },
            ["Russian"] = new[]
            {
                "Зимние сани",
                "Грузовые сани на полозьях, подбитых железом. Легко скользят по снегу и льду, но тяжело волочатся по голой земле. Сядьте в них, чтобы скатиться со снежного склона.",
                "Сесть", "руль", "тормоз", "оттолкнуться", "сойти",
                "Тяну сани", "Полозья скользят по снегу и льду, по голой земле сани идут тяжело.",
                "Снег", "Сугробы", "Лёд", "Дерево", "Камень", "Земля", "Вода",
                "Колёса в снегу", "Колёса телеги вязнут в снегу и тормозят вас. Сани здесь бы скользили.",
                "Мёрзлая земля",
            },
            ["Ukrainian"] = new[]
            {
                "Зимові сани",
                "Вантажні сани на полозах, підбитих залізом. Легко ковзають снігом і кригою, але важко тягнуться голою землею. Сядьте в них, щоб з'їхати зі сніжного схилу.",
                "Сісти", "кермо", "гальмо", "відштовхнутися", "зійти",
                "Тягну сани", "Полози ковзають снігом і кригою, голою землею сани йдуть важко.",
                "Сніг", "Замети", "Крига", "Дерево", "Камінь", "Земля", "Вода",
                "Колеса в снігу", "Колеса воза грузнуть у снігу й гальмують вас. Сани тут ковзали б.",
                "Мерзла земля",
            },
            ["Bulgarian"] = new[]
            {
                "Зимна шейна",
                "Товарна шейна с плазове, обковани с желязо. Плъзга се леко по сняг и лед, но се влачи тежко по гола земя. Седнете в нея, за да се спуснете по заснежен склон.",
                "Седни", "завой", "спирачка", "отблъсни се", "слез",
                "Тегля шейна", "Плазовете се плъзгат по сняг и лед; голата земя ви забавя.",
                "Сняг", "Дълбок сняг", "Лед", "Дърво", "Камък", "Земя", "Вода",
                "Колела в снега", "Колелата на количката затъват в снега и ви забавят. Шейна би се плъзгала тук.",
                "Замръзнала земя",
            },
            ["Macedonian"] = new[]
            {
                "Зимски санки",
                "Товарни санки со лизгалки оковани со железо. Лесно се лизгаат по снег и мраз, но тешко се влечат по гола земја. Седнете во нив за да се спуштите по снежна падина.",
                "Седни", "свртување", "сопирање", "оттурни се", "симни се",
                "Влечам санки", "Лизгалките се лизгаат по снег и мраз; голата земја ве успорува.",
                "Снег", "Длабок снег", "Мраз", "Дрво", "Камен", "Земја", "Вода",
                "Тркала во снег", "Тркалата на количката пропаѓаат во снегот и ве успоруваат. Санките тука би се лизгале.",
                "Замрзната земја",
            },
            ["Serbian"] = new[]
            {
                "Зимске санке",
                "Теретне санке са клизачима окованим гвожђем. Лако клизе по снегу и леду, али се тешко вуку по голој земљи. Седите у њих да се спустите низ снежну падину.",
                "Седи", "скретање", "кочење", "одгурни се", "сиђи",
                "Вучем санке", "Клизачи клизе по снегу и леду; гола земља вас успорава.",
                "Снег", "Дубок снег", "Лед", "Дрво", "Камен", "Земља", "Вода",
                "Точкови у снегу", "Точкови колица пропадају у снег и успоравају вас. Санке би овде клизиле.",
                "Смрзнута земља",
            },
            ["Croatian"] = new[]
            {
                "Zimske saonice",
                "Teretne saonice s klizačima okovanim željezom. Lako klize po snijegu i ledu, ali se teško vuku po goloj zemlji. Sjednite u njih da se spustite niz snježnu padinu.",
                "Sjedni", "skretanje", "kočenje", "odgurni se", "siđi",
                "Vučem saonice", "Klizači klize po snijegu i ledu; gola zemlja vas usporava.",
                "Snijeg", "Dubok snijeg", "Led", "Drvo", "Kamen", "Zemlja", "Voda",
                "Kotači u snijegu", "Kotači kolica propadaju u snijeg i usporavaju vas. Saonice bi ovdje klizile.",
                "Smrznuto tlo",
            },
            ["Polish"] = new[]
            {
                "Zimowe sanie",
                "Towarowe sanie na płozach okutych żelazem. Lekko suną po śniegu i lodzie, ale ciężko wloką się po gołej ziemi. Usiądź w nich, by zjechać ze śnieżnego stoku.",
                "Usiądź", "skręt", "hamulec", "odepchnij się", "zejdź",
                "Ciągnę sanie", "Płozy suną po śniegu i lodzie; goła ziemia cię spowalnia.",
                "Śnieg", "Głęboki śnieg", "Lód", "Drewno", "Kamień", "Ziemia", "Woda",
                "Koła w śniegu", "Koła wózka grzęzną w śniegu i cię spowalniają. Sanie by tu sunęły.",
                "Zmarznięta ziemia",
            },
            ["Czech"] = new[]
            {
                "Zimní saně",
                "Nákladní saně se železem okovanými ližinami. Lehce kloužou po sněhu a ledu, ale po holé zemi se táhnou ztěžka. Posaďte se do nich a sjeďte zasněžený svah.",
                "Sednout", "řízení", "brzda", "odrazit se", "vystoupit",
                "Táhnu saně", "Ližiny kloužou po sněhu a ledu; holá zem vás zpomaluje.",
                "Sníh", "Hluboký sníh", "Led", "Dřevo", "Kámen", "Země", "Voda",
                "Kola ve sněhu", "Kola vozíku se boří do sněhu a zpomalují vás. Saně by tu klouzaly.",
                "Zmrzlá půda",
            },
            ["Slovak"] = new[]
            {
                "Zimné sane",
                "Nákladné sane so železom okovanými sanicami. Ľahko kĺžu po snehu a ľade, ale po holej zemi sa ťahajú ťažko. Sadnite si do nich a zíďte po zasneženom svahu.",
                "Sadnúť", "riadenie", "brzda", "odraziť sa", "vystúpiť",
                "Ťahám sane", "Sanice kĺžu po snehu a ľade; holá zem vás spomaľuje.",
                "Sneh", "Hlboký sneh", "Ľad", "Drevo", "Kameň", "Zem", "Voda",
                "Kolesá v snehu", "Kolesá vozíka sa boria do snehu a spomaľujú vás. Sane by tu kĺzali.",
                "Zamrznutá pôda",
            },
            ["German"] = new[]
            {
                "Winterschlitten",
                "Ein Lastschlitten auf eisenbeschlagenen Kufen. Er gleitet über Schnee und Eis, schleift aber schwer über blanken Boden. Setz dich hinein, um einen verschneiten Hang hinabzufahren.",
                "Einsteigen", "lenken", "bremsen", "abstoßen", "aussteigen",
                "Schlitten ziehen", "Kufen gleiten auf Schnee und Eis; blanker Boden bremst dich.",
                "Schnee", "Tiefschnee", "Eis", "Holz", "Stein", "Boden", "Wasser",
                "Räder im Schnee", "Die Räder des Karrens sinken in den Schnee ein und bremsen dich. Ein Schlitten würde hier gleiten.",
                "Gefrorener Boden",
            },
            ["Dutch"] = new[]
            {
                "Winterslee",
                "Een vrachtslee op met ijzer beslagen glijders. Hij glijdt over sneeuw en ijs, maar sleept zwaar over kale grond. Ga erin zitten om een besneeuwde helling af te rijden.",
                "Instappen", "sturen", "remmen", "afzetten", "uitstappen",
                "Slee trekken", "Glijders glijden over sneeuw en ijs; kale grond remt je af.",
                "Sneeuw", "Diepe sneeuw", "IJs", "Hout", "Steen", "Grond", "Water",
                "Wielen in de sneeuw", "De wielen van de kar zakken weg in de sneeuw en remmen je af. Een slee zou hier glijden.",
                "Bevroren grond",
            },
            ["Swedish"] = new[]
            {
                "Vintersläde",
                "En lastsläde på järnskodda medar. Den glider över snö och is men släpar tungt över bar mark. Sätt dig i den för att åka nerför en snöig sluttning.",
                "Åk", "styr", "broms", "skjut ifrån", "kliv av",
                "Drar släde", "Medarna glider på snö och is; bar mark bromsar dig.",
                "Snö", "Djupsnö", "Is", "Trä", "Sten", "Mark", "Vatten",
                "Hjul i snön", "Vagnens hjul sjunker ner i snön och bromsar dig. En släde skulle glida här.",
                "Tjälad mark",
            },
            ["Danish"] = new[]
            {
                "Vinterslæde",
                "En lastslæde på jernbeslåede meder. Den glider over sne og is, men slæber tungt over bar jord. Sæt dig i den for at køre ned ad en snedækket skråning.",
                "Kør", "styr", "brems", "skub fra", "stig af",
                "Trækker slæde", "Mederne glider på sne og is; bar jord bremser dig.",
                "Sne", "Dyb sne", "Is", "Træ", "Sten", "Jord", "Vand",
                "Hjul i sneen", "Vognens hjul synker ned i sneen og bremser dig. En slæde ville glide her.",
                "Frossen jord",
            },
            ["Norwegian"] = new[]
            {
                "Vinterslede",
                "En lastslede på jernskodde meier. Den glir over snø og is, men sleper tungt over bar mark. Sett deg i den for å ake ned en snødekt skråning.",
                "Kjør", "styr", "brems", "skyv fra", "gå av",
                "Drar slede", "Meiene glir på snø og is; bar mark bremser deg.",
                "Snø", "Dyp snø", "Is", "Tre", "Stein", "Mark", "Vann",
                "Hjul i snøen", "Vognhjulene synker ned i snøen og bremser deg. En slede ville gli her.",
                "Frossen mark",
            },
            ["Icelandic"] = new[]
            {
                "Vetrarsleði",
                "Flutningasleði á járnslegnum meiðum. Hann rennur yfir snjó og ís en dregst þungt yfir bera jörð. Sestu í hann til að renna niður snævi þakta brekku.",
                "Setjast", "stýra", "hemla", "spyrna", "stíga af",
                "Dreg sleða", "Meiðarnir renna á snjó og ís; ber jörð hægir á þér.",
                "Snjór", "Djúpur snjór", "Ís", "Viður", "Steinn", "Jörð", "Vatn",
                "Hjól í snjó", "Hjól kerrunnar sökkva í snjóinn og hægja á þér. Sleði myndi renna hér.",
                "Frosin jörð",
            },
            ["Finnish"] = new[]
            {
                "Talvireki",
                "Raudoitetuilla jalaksilla kulkeva tavarareki. Se liukuu lumella ja jäällä, mutta laahautuu raskaasti paljaalla maalla. Istu siihen laskeaksesi lumista rinnettä alas.",
                "Istu", "ohjaa", "jarruta", "potkaise vauhtia", "nouse pois",
                "Vedän rekeä", "Jalakset liukuvat lumella ja jäällä; paljas maa hidastaa sinua.",
                "Lumi", "Syvä lumi", "Jää", "Puu", "Kivi", "Maa", "Vesi",
                "Pyörät lumessa", "Kärryjen pyörät uppoavat lumeen ja hidastavat sinua. Reki liukuisi tässä.",
                "Jäätynyt maa",
            },
            ["French"] = new[]
            {
                "Traîneau d'hiver",
                "Un traîneau de charge aux patins ferrés. Il glisse sur la neige et la glace mais se traîne lourdement sur la terre nue. Asseyez-vous dedans pour dévaler une pente enneigée.",
                "Monter", "diriger", "freiner", "pousser", "descendre",
                "Traîne un traîneau", "Les patins glissent sur la neige et la glace ; la terre nue vous ralentit.",
                "Neige", "Neige profonde", "Glace", "Bois", "Pierre", "Sol", "Eau",
                "Roues dans la neige", "Les roues de la charrette s'enfoncent dans la neige et vous ralentissent. Un traîneau glisserait ici.",
                "Sol gelé",
            },
            ["Italian"] = new[]
            {
                "Slitta invernale",
                "Una slitta da carico su pattini ferrati. Scivola su neve e ghiaccio ma si trascina a fatica sul terreno nudo. Siediti dentro per scendere un pendio innevato.",
                "Sali", "sterza", "frena", "spingiti", "scendi",
                "Traini una slitta", "I pattini scivolano su neve e ghiaccio; il terreno nudo ti rallenta.",
                "Neve", "Neve alta", "Ghiaccio", "Legno", "Pietra", "Terreno", "Acqua",
                "Ruote nella neve", "Le ruote del carretto affondano nella neve e ti rallentano. Una slitta qui scivolerebbe.",
                "Terreno ghiacciato",
            },
            ["Spanish"] = new[]
            {
                "Trineo de invierno",
                "Un trineo de carga con patines herrados. Se desliza sobre la nieve y el hielo, pero se arrastra con dificultad sobre la tierra desnuda. Siéntate en él para bajar por una ladera nevada.",
                "Subir", "girar", "frenar", "impulsarse", "bajar",
                "Arrastras un trineo", "Los patines se deslizan sobre nieve y hielo; la tierra desnuda te frena.",
                "Nieve", "Nieve profunda", "Hielo", "Madera", "Piedra", "Tierra", "Agua",
                "Ruedas en la nieve", "Las ruedas del carro se hunden en la nieve y te frenan. Un trineo se deslizaría aquí.",
                "Suelo helado",
            },
            ["Portuguese_European"] = new[]
            {
                "Trenó de inverno",
                "Um trenó de carga com patins ferrados. Desliza sobre a neve e o gelo, mas arrasta-se pesadamente sobre terra nua. Senta-te nele para descer uma encosta nevada.",
                "Montar", "virar", "travar", "dar impulso", "sair",
                "A puxar um trenó", "Os patins deslizam na neve e no gelo; a terra nua abranda-te.",
                "Neve", "Neve funda", "Gelo", "Madeira", "Pedra", "Terra", "Água",
                "Rodas na neve", "As rodas da carroça enterram-se na neve e abrandam-te. Um trenó deslizaria aqui.",
                "Solo gelado",
            },
            ["Portuguese_Brazilian"] = new[]
            {
                "Trenó de inverno",
                "Um trenó de carga com patins ferrados. Desliza sobre neve e gelo, mas se arrasta com dificuldade sobre terra nua. Sente-se nele para descer uma encosta nevada.",
                "Montar", "virar", "frear", "dar impulso", "descer",
                "Puxando um trenó", "Os patins deslizam na neve e no gelo; a terra nua te deixa mais lento.",
                "Neve", "Neve funda", "Gelo", "Madeira", "Pedra", "Terra", "Água",
                "Rodas na neve", "As rodas do carrinho afundam na neve e te deixam mais lento. Um trenó deslizaria aqui.",
                "Solo congelado",
            },
            ["Romanian"] = new[]
            {
                "Sanie de iarnă",
                "O sanie de povară cu tălpi ferecate cu fier. Alunecă pe zăpadă și gheață, dar se târăște greu pe pământ gol. Așază-te în ea ca să cobori o pantă înzăpezită.",
                "Urcă", "virează", "frânează", "împinge-te", "coboară",
                "Tragi o sanie", "Tălpile alunecă pe zăpadă și gheață; pământul gol te încetinește.",
                "Zăpadă", "Zăpadă adâncă", "Gheață", "Lemn", "Piatră", "Pământ", "Apă",
                "Roți în zăpadă", "Roțile căruței se afundă în zăpadă și te încetinesc. O sanie ar aluneca aici.",
                "Pământ înghețat",
            },
            ["Hungarian"] = new[]
            {
                "Téli szán",
                "Vasalt talpú teherszán. Könnyedén siklik havon és jégen, de csupasz talajon nehezen vonszolható. Ülj bele, hogy lecsússz egy havas lejtőn.",
                "Beülés", "kormányzás", "fékezés", "ellökés", "kiszállás",
                "Szánt húzol", "A talpak siklanak havon és jégen; a csupasz talaj lelassít.",
                "Hó", "Mély hó", "Jég", "Fa", "Kő", "Talaj", "Víz",
                "Kerekek a hóban", "A kocsi kerekei besüppednek a hóba és lelassítanak. Egy szán itt siklana.",
                "Fagyott talaj",
            },
            ["Lithuanian"] = new[]
            {
                "Žiemos rogės",
                "Krovininės rogės geležimi kaustytomis pavažomis. Lengvai slysta sniegu ir ledu, bet sunkiai velkasi plika žeme. Atsisėskite į jas ir nusileiskite snieguotu šlaitu.",
                "Sėsti", "vairuoti", "stabdyti", "atsispirti", "išlipti",
                "Traukiu roges", "Pavažos slysta sniegu ir ledu; plika žemė jus lėtina.",
                "Sniegas", "Gilus sniegas", "Ledas", "Mediena", "Akmuo", "Žemė", "Vanduo",
                "Ratai sniege", "Vežimo ratai klimpsta sniege ir jus lėtina. Rogės čia slystų.",
                "Įšalusi žemė",
            },
            ["Latvian"] = new[]
            {
                "Ziemas ragavas",
                "Kravas ragavas ar dzelzi kaltām slidām. Viegli slīd pa sniegu un ledu, bet smagi velkas pa kailu zemi. Apsēdies tajās, lai nobrauktu pa sniegotu nogāzi.",
                "Sēsties", "stūrēt", "bremzēt", "atgrūsties", "izkāpt",
                "Velku ragavas", "Slidas slīd pa sniegu un ledu; kaila zeme tevi palēnina.",
                "Sniegs", "Dziļš sniegs", "Ledus", "Koks", "Akmens", "Zeme", "Ūdens",
                "Riteņi sniegā", "Ratu riteņi grimst sniegā un tevi palēnina. Ragavas te slīdētu.",
                "Sasalusi zeme",
            },
            ["Turkish"] = new[]
            {
                "Kış Kızağı",
                "Demir kaplı paletleri olan bir yük kızağı. Kar ve buz üzerinde kayar ama çıplak toprakta ağır ağır sürüklenir. Karlı bir yamaçtan kaymak için içine otur.",
                "Bin", "yönlendir", "fren", "ittir", "in",
                "Kızak çekiyorsun", "Paletler kar ve buzda kayar; çıplak toprak seni yavaşlatır.",
                "Kar", "Derin kar", "Buz", "Odun", "Taş", "Toprak", "Su",
                "Karda tekerlekler", "El arabasının tekerlekleri karda batar ve seni yavaşlatır. Kızak burada kayardı.",
                "Donmuş toprak",
            },
            ["Greek"] = new[]
            {
                "Χειμερινό έλκηθρο",
                "Ένα έλκηθρο φορτίου με σιδερένια πέλματα. Γλιστράει πάνω σε χιόνι και πάγο, αλλά σέρνεται βαριά σε γυμνό έδαφος. Κάθισε μέσα για να κατέβεις μια χιονισμένη πλαγιά.",
                "Κάθισε", "στρίψε", "φρένο", "σπρώξε", "κατέβα",
                "Σέρνεις έλκηθρο", "Τα πέλματα γλιστρούν σε χιόνι και πάγο· το γυμνό έδαφος σε επιβραδύνει.",
                "Χιόνι", "Βαθύ χιόνι", "Πάγος", "Ξύλο", "Πέτρα", "Έδαφος", "Νερό",
                "Τροχοί στο χιόνι", "Οι τροχοί του κάρου βουλιάζουν στο χιόνι και σε επιβραδύνουν. Ένα έλκηθρο θα γλιστρούσε εδώ.",
                "Παγωμένο έδαφος",
            },
            ["Georgian"] = new[]
            {
                "ზამთრის მარხილი",
                "სატვირთო მარხილი რკინით მოჭედილი თოხებით. თოვლსა და ყინულზე ადვილად სრიალებს, მაგრამ შიშველ მიწაზე ძნელად მიათრევ. ჩაჯექი, რომ თოვლიან ფერდობზე დაეშვა.",
                "ჩაჯდომა", "მართვა", "დამუხრუჭება", "ბიძგი", "ჩამოსვლა",
                "მარხილს ეწევი", "თოხები თოვლსა და ყინულზე სრიალებს; შიშველი მიწა გაგანელებს.",
                "თოვლი", "ღრმა თოვლი", "ყინული", "ხე", "ქვა", "მიწა", "წყალი",
                "ბორბლები თოვლში", "ურმის ბორბლები თოვლში იფლობა და გაგანელებს. მარხილი აქ გასრიალდებოდა.",
                "გაყინული მიწა",
            },
            ["Hindi"] = new[]
            {
                "शीतकालीन स्लेज",
                "लोहे से मढ़े रनर्स वाली माल-ढुलाई स्लेज। यह बर्फ़ और हिम पर फिसलती है, पर नंगी ज़मीन पर भारी घिसटती है। बर्फ़ीली ढलान से नीचे फिसलने के लिए इसमें बैठें।",
                "बैठें", "मोड़ें", "ब्रेक", "धक्का दें", "उतरें",
                "स्लेज खींच रहे हैं", "रनर्स बर्फ़ और हिम पर फिसलते हैं; नंगी ज़मीन आपको धीमा करती है।",
                "बर्फ़", "गहरी बर्फ़", "हिम", "लकड़ी", "पत्थर", "ज़मीन", "पानी",
                "बर्फ़ में पहिए", "गाड़ी के पहिए बर्फ़ में धँसते हैं और आपको धीमा करते हैं। यहाँ स्लेज फिसलती।",
                "जमी हुई ज़मीन",
            },
            ["Thai"] = new[]
            {
                "เลื่อนฤดูหนาว",
                "เลื่อนบรรทุกสินค้าที่มีแคร่หุ้มเหล็ก ไถลไปบนหิมะและน้ำแข็งได้ดี แต่ลากบนพื้นดินโล่งได้ยาก นั่งลงไปเพื่อไถลลงเนินหิมะ",
                "นั่ง", "บังคับ", "เบรก", "ถีบส่ง", "ลงจากเลื่อน",
                "กำลังลากเลื่อน", "แคร่ไถลบนหิมะและน้ำแข็ง พื้นดินโล่งทำให้คุณช้าลง",
                "หิมะ", "หิมะลึก", "น้ำแข็ง", "ไม้", "หิน", "พื้นดิน", "น้ำ",
                "ล้อติดหิมะ", "ล้อรถเข็นจมลงในหิมะและทำให้คุณช้าลง ถ้าเป็นเลื่อนจะไถลไปได้",
                "พื้นดินเยือกแข็ง",
            },
            ["Chinese"] = new[]
            {
                "冬季雪橇",
                "装有包铁滑板的载货雪橇。在雪地和冰面上轻快滑行,在裸露的地面上却拖得很吃力。坐进去就能滑下雪坡。",
                "乘坐", "转向", "刹车", "蹬地", "下车",
                "拉着雪橇", "滑板在雪地和冰面上滑行;裸露的地面会让你变慢。",
                "雪地", "深雪", "冰面", "木头", "石头", "地面", "水",
                "车轮陷雪", "板车的车轮陷进雪里,拖慢了你。换成雪橇就能在这里滑行。",
                "冻土",
            },
            ["Chinese_Trad"] = new[]
            {
                "冬季雪橇",
                "裝有包鐵滑板的載貨雪橇。在雪地和冰面上輕快滑行,在裸露的地面上卻拖得很吃力。坐進去就能滑下雪坡。",
                "乘坐", "轉向", "煞車", "蹬地", "下車",
                "拉著雪橇", "滑板在雪地和冰面上滑行;裸露的地面會讓你變慢。",
                "雪地", "深雪", "冰面", "木頭", "石頭", "地面", "水",
                "車輪陷雪", "推車的車輪陷進雪裡,拖慢了你。換成雪橇就能在這裡滑行。",
                "凍土",
            },
            ["Japanese"] = new[]
            {
                "冬のそり",
                "鉄張りのランナーを備えた荷運び用のそり。雪や氷の上はすべるように進むが、むき出しの地面では重く引きずることになる。乗り込めば雪の斜面を滑り降りられる。",
                "乗る", "かじ取り", "ブレーキ", "蹴り出す", "降りる",
                "そりを引いている", "ランナーは雪や氷の上で滑る。むき出しの地面では足が遅くなる。",
                "雪", "深雪", "氷", "木", "石", "地面", "水",
                "車輪が雪に埋まる", "荷車の車輪が雪に沈み、足が遅くなる。そりならここを滑っていける。",
                "凍土",
            },
            ["Korean"] = new[]
            {
                "겨울 썰매",
                "쇠를 덧댄 활주부가 달린 짐 썰매. 눈과 얼음 위에서는 미끄러지듯 나아가지만, 맨땅에서는 무겁게 끌린다. 올라타면 눈 덮인 비탈을 미끄러져 내려갈 수 있다.",
                "타기", "방향 전환", "제동", "밀어내기", "내리기",
                "썰매를 끄는 중", "활주부는 눈과 얼음 위에서 미끄러진다. 맨땅에서는 느려진다.",
                "눈", "깊은 눈", "얼음", "나무", "돌", "땅", "물",
                "눈에 빠진 바퀴", "수레 바퀴가 눈에 빠져 속도가 느려진다. 썰매라면 여기서 미끄러질 것이다.",
                "언 땅",
            },
        };

        // the hint shown when sitting down: the player's own key bindings, then the words
        public const string ControlsHint =
            "[$KEY_Left/$KEY_Right] $wintersled_steer   [$KEY_Backward] $wintersled_brake   [$KEY_Forward] $wintersled_push   [$KEY_Jump] $wintersled_getoff";

        public static void Register()
        {
            var loc = LocalizationManager.Instance.GetLocalization();
            foreach (var row in Rows)
            {
                var dict = new Dictionary<string, string>();
                for (int i = 0; i < Keys.Length && i < row.Value.Length; i++)
                {
                    dict[Keys[i]] = row.Value[i];
                }
                loc.AddTranslation(row.Key, dict);
            }
        }

        public static IEnumerable<string> Languages => Rows.Keys;
    }
}
