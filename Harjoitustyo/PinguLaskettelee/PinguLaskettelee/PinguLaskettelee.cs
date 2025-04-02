using System.Collections.Generic;
using Jypeli;

namespace PinguLaskettelee;

/// @author Riku Rönkä
/// @version 31.03.2025
/// <summary>
/// Pingu Laskettelee! on yksinpelattava scroller peli, jossa
/// pelaaja välttelee satunnaisia esteitä ja pyrkii pääsemään maaliin
/// vahingoittumatta.
/// </summary>
public class PinguLaskettelee : PhysicsGame
{
    /// <summary>
    /// Pingun sydänten maksimi
    /// Pelin etenemisen nopeutta määrää laskuri. Nämä rajoittavat laskurin välille 0 ja 3.
    /// Samalla toimivat sydänten rajoina.
    /// </summary>
    private const int SydantenMaksimi = 3;
    /// <summary>
    /// Pingun sydänten minimi
    /// Pelin etenemisen nopeutta määrää laskuri. Nämä rajoittavat laskurin välille 0 ja 3.
    /// Samalla toimivat sydänten rajoina.
    /// </summary>
    private const int SydantenMinimi = 0;
    
    // Pingun kuvia eri sydänmäärillä
    private static readonly Image pingunKuvaKolme = LoadImage("pinguKolme.png");
    private static readonly Image pingunKuvaKaksi = LoadImage("pinguKaksi.png");
    private static readonly Image pingunKuvaYksi = LoadImage("pinguYksi.png");
    private static readonly Image pingunKuvaNolla = LoadImage("pinguNolla.png");
    
    // Kiven kuva
    private static readonly Image kiviYksi = LoadImage("kivi.png");
    // Puun kuva
    private static readonly Image puuYksi = LoadImage("puu.png");
    
    
    // Luodaan esteiden muodot kuvista. 
    // On ikävää, jos esteen muoto onkin erilainen, kun kuvasta päättelisi.
    // Myös ikävää, kun peli ei pyöri kunnolla näiden kanssa
    // TODO ~ ETSI VIKA
    /*
    private Shape kiviMuoto = Shape.FromImage(kiviYksi);
    private Shape puuMuoto = Shape.FromImage(puuYksi);
    private Shape pinguMuoto = Shape.FromImage(pingunKuvaKolme);
    */

    /// <summary>
    /// Pingun sydänten lukumäärä
    /// </summary>
    private int _pingunSydamet;
    /// <summary>
    /// Esteiden nopeus kokonaislukuna. Kasvaa pelin edetessä
    /// ja pienenee pingun osuessa esteeseen
    /// </summary>
    private int _esteenNopeus;
    
    // Pingu
    private PhysicsObject pingu;

    /// <summary>
    /// Lista sydän fysiikkaobjekteista
    /// </summary>
    private List<PhysicsObject> pingunSydametListana;
    /// <summary>
    /// Lista este fysiikkaobjekteista
    /// </summary>
    private List<PhysicsObject> kentanEsteetListana;
    private List<PhysicsObject> tahdetListana;
    /// <summary>
    /// Ajastin, joka hoitaa uusien objektien syntymisen ja nopeuden
    /// tietyin väliajoin
    /// </summary>
    private Timer ajastin;
    /// <summary>
    /// Laskee peliaikaa
    /// </summary>
    private Timer aikaLaskuri;
    
    /// <summary>
    /// Laskee sydämiä ruudulle näytettäväksi
    /// </summary>
    public IntMeter sydanLaskuri;

    public IntMeter tahtiLaskuri;
    

    /// <summary>
    /// Pääohjelma, joka aloittaa pelin.
    /// Kutsuu LuoKentta aliohjelmaa, joka luo kentän, asettaa kameran, ja luo objektit.
    /// </summary>
    public override void Begin()
    {
        LuoKentta();
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }

    
    /// <summary>
    /// LuoKentta luo kentän reunat, taustavärin, Pingun
    /// kameran, laskurin, ohjaimet, esteitä muutaman
    /// </summary>
    private void LuoKentta()
    {
        IsFullScreen = true;
        Vector kentanKoko = new Vector(Screen.Width, Screen.Height);
        Level.Size = kentanKoko;
        Level.CreateLeftBorder(0, true);
        Level.CreateRightBorder(0, true);
        Level.BackgroundColor = Color.White;
        LuoMuuttujat();
        pingu = Pingu();
        
        // TÄLLÄ SAA HUOMATTAVASTI VAIKEUTETTUA TÄTÄ PELIÄ! 
        // EI KANNATA ZOOMIA PIENENTÄÄ, NÄYTTÄÄ RUMALTA
        /*Camera.FollowedObject = pingu; 
        Camera.ZoomFactor = 2;*/
        
        LisaaLaskuri();
        AsetaOhjaimet();
        AjastinUpdate();
    }
    
    
    /// <summary>
    /// LuoMuuttujat aliohjelma hoitaa tarvittavien muuttujien arvojen asettamisen
    /// pelin alussa. 
    /// </summary>
    private void LuoMuuttujat()
    {

        _pingunSydamet = 3;
        _esteenNopeus = 300;

        tahdetListana = new List<PhysicsObject>();
        pingunSydametListana = new List<PhysicsObject>();
        kentanEsteetListana = new List<PhysicsObject>();
    }
    
    
    /// <summary>
    /// Lisätään joitakin laskureita kentälle
    /// </summary>
    private void LisaaLaskuri()
    {
        sydanLaskuri = LuoSydanLaskuri();
        tahtiLaskuri = LuoTahtiLaskuri();
        LisaaAikaLaskuri();
    }


    /// <summary>
    /// Lisää aikalaskurin
    /// </summary>
    private void LisaaAikaLaskuri()
    {
        aikaLaskuri = new Timer();
        aikaLaskuri.Start();
        Label aikaNaytolla = new Label();
        aikaNaytolla.Title = "Aika: ";
        aikaNaytolla.DecimalPlaces = 1;
        aikaNaytolla.BindTo(aikaLaskuri.SecondCounter);
        aikaNaytolla.X = Screen.Right - 100;
        aikaNaytolla.Y = Screen.Top - 50;
        Add(aikaNaytolla);
    }
    
    
    /// <summary>
    /// Aliohjelma luo sydänlaskurin, ja lisää sen ruudulle
    /// </summary>
    /// <returns>Palauttaa IntMeter laskurin</returns>
    private IntMeter LuoSydanLaskuri()
    {
        IntMeter laskuri = new IntMeter(3);
        laskuri.MinValue = SydantenMinimi;
        laskuri.MaxValue = SydantenMaksimi;
        
        Label sydanLaskuriNaytolla = new Label();
        sydanLaskuriNaytolla.Title = "Sydamet: ";
        sydanLaskuriNaytolla.BindTo(laskuri);
        sydanLaskuriNaytolla.X = 0;
        sydanLaskuriNaytolla.Y = Level.Top - 50;
        sydanLaskuriNaytolla.TextColor = Color.Black;
        sydanLaskuriNaytolla.BorderColor = Color.White;
        sydanLaskuriNaytolla.Color = Color.White;
        Add(sydanLaskuriNaytolla);
        
        return laskuri;
    }
    
    
    /// <summary>
    /// Luo tähtilaskurin
    /// TODO ~ Yhdistä toiseen aliohjelmaan...
    /// </summary>
    /// <returns></returns>
    private IntMeter LuoTahtiLaskuri()
    {
        IntMeter laskuri = new IntMeter(0);
        laskuri.MinValue = SydantenMinimi;
        
        Label tahtiLaskuriNaytolla = new Label();
        tahtiLaskuriNaytolla.Title = "Tahdet: ";
        tahtiLaskuriNaytolla.BindTo(laskuri);
        tahtiLaskuriNaytolla.X = 0;
        tahtiLaskuriNaytolla.Y = Level.Top - 100;
        tahtiLaskuriNaytolla.TextColor = Color.Black;
        tahtiLaskuriNaytolla.BorderColor = Color.White;
        tahtiLaskuriNaytolla.Color = Color.White;
        Add(tahtiLaskuriNaytolla);
        
        return laskuri;
    }
    
    
    /// <summary>
    /// Asettaa Pingun ohjaimet.
    /// Näppäimistön A liikuttaa vasemmalle, D liikuttaa oikealle.
    /// </summary>
    private void AsetaOhjaimet()
    {
        int pingunNopeus = 700;
        Keyboard.Listen(Key.A, ButtonState.Down, LiikutaPingua, "Liikuttaa pingua vasemmalle", pingu, new Vector(-pingunNopeus, 0));
        Keyboard.Listen(Key.D, ButtonState.Down, LiikutaPingua, "Liikuttaa pingua oikealle", pingu, new Vector(pingunNopeus, 0));
    }


    /// <summary>
    /// TODO ~ Poista kulmanopeus parametri tai hyödynnä sitä!
    /// Suorittaa Pingun liikuttamisen näppäimistön painalluksissa. 
    /// </summary>
    /// <param name="pelaaja">Mitä liikutetaan, eli pingu</param>
    /// <param name="suunta">Mihin suuntaan liikutetaan</param>
    private void LiikutaPingua(PhysicsObject pelaaja, Vector suunta)
    {
        pingu.Push(suunta);
    }
    
    
    /// <summary>
    /// Luodaan tietyn väliajoin päivittävä laskuri,
    /// joka kutsuu Laskin aliohjelmaa tietyn ajan välein
    /// </summary>
    private void AjastinUpdate()
    {
        // Asetetaan aluksi ajastimen toimien väli kerran per sekunti
        double ajastimenIntervalli = 1; 
        // Luodaan ajastin
        ajastin = new Timer();
        ajastin.Interval = ajastimenIntervalli;
        ajastin.Timeout += AjastimenTapahtumat;
        ajastin.Start();
    }

    
    /// <summary>
    /// Laskin kertoo, mitä tehdään aina ajastimen mukaisen intervallin aikana.
    /// Laskin nostaa objektien nopeutta, vähentää ajastimen tapahtumien aikaväliä,
    /// luo uusia objekteja ja tuhoaa kentän ohi menneitä
    /// </summary>
    private void AjastimenTapahtumat()
    {
        // Nämä kokonaisluvut luovat satunnaisuutta esteiden syntymiseen
        int satunnainenArvo = RandomGen.NextInt(0, 100);
        int satunnaisuudenProsentti = 99;
        
        // Paljonko lisätään esteen nopeutta per ajastimen intervalli
        int esteenNopeudenLisays = 10;
        
        // Näitä muokkaamalla määrää montako estettä syntyy 
        // kun ajastin saavuttaa intervallin
        int montakoEstettaSyntyyVahintaan = 3;
        int montakoEstettaSyntyyEnintaan = 10;
        
        // Jottei kuoleman jälkeen jotkut objektit jatka liikettä
        if (_pingunSydamet == 0) _esteenNopeus = 0;
        
        // Vähennetään ajastimen tapahtumien välistä aikaa
        // Tapahtuu niin 
        if (ajastin.Interval > 0.5) ajastin.Interval -= 0.01;
        //Console.WriteLine("AJASTIN "+ajastin.Interval);
        //Console.WriteLine("interval on "+interval);
        //kuljettuMatka += _ajastimenIntervalli;
        //Console.WriteLine(este[3].Y);
        
        // Tässä luodaan esteitä, jos satunnainnaisest luotu kokonaisluku on alle raja-arvon
        // ja sydämiä on enemmän kuin 0
        if (satunnainenArvo < satunnaisuudenProsentti && _pingunSydamet > 0)
        {
            int random2 = RandomGen.NextInt(montakoEstettaSyntyyVahintaan, montakoEstettaSyntyyEnintaan);
            kentanEsteetListana = LuoEste(random2);
            tahdetListana = LuoTahti(random2-3);
        }
        
        // Tässä luodaan sydämiä, jos satunnainnaisest luotu kokonaisluku on alle raja-arvon
        // ja sydämiä on enemmän kuin 0
        if (satunnainenArvo < satunnaisuudenProsentti && _pingunSydamet is < 3 and > 0)
        {
            pingunSydametListana = LuoSydan();
        }
        
        // Objektien nopeus vektoriksi
        _esteenNopeus += esteenNopeudenLisays;
        Vector esteidenNopeusVektorina = new Vector(0, _esteenNopeus);
        // Muutetaan objektien nopeuksia
        TarkastaObjektienNopeus(esteidenNopeusVektorina);
    }

    
    /// <summary>
    /// Pelaajan liikuttama fysiikkaolio Pingu
    /// </summary>
    /// <returns>Palauttaa PhysicsObject pingun</returns>
    private PhysicsObject Pingu()
    {
        Vector pingunSijaintiAluksi = new Vector(0, 200);
        int pingunKoko = 50;
        double pingunLinearDamping = 0.998;
        
        pingu = new PhysicsObject(pingunKoko, pingunKoko);
        pingu.Shape = Shape.Circle;
        pingu.Color = Color.Red;
        pingu.Position = pingunSijaintiAluksi;
        pingu.Image = pingunKuvaKolme;
        pingu.IgnoresCollisionResponse = false;
        pingu.LinearDamping = pingunLinearDamping;
        //AddCollisionHandler(pingu, Tormays);
        AddCollisionHandler(pingu, "este", TormaysEste);
        AddCollisionHandler(pingu, "sydan", TormaysSydan);
        AddCollisionHandler(pingu, "tahti", TormaysTahti);
        Add(pingu);
        return pingu;
    }

    
    /// <summary>
    /// Hoitaa fysiikkaobjektin luomisen parametrien avulla
    /// </summary>
    /// <param name="objektinKorkeus">Kuinka korkea objekti</param>
    /// <param name="objektinLeveys">Kuinka leveä objekti</param>
    /// <param name="muoto">objektin muoto</param>
    /// <param name="vari">objektin väri</param>
    /// <param name="objektinTagi">objektin tagi</param>
    /// <returns>Luotu fysiikkaobjekti</returns>
    private PhysicsObject LuoFysiikkaObjekti(int objektinKorkeus, int objektinLeveys, Shape muoto, Color vari, string objektinTagi)
    {
        double randomX = RandomGen.NextDouble(Level.Left, Level.Right);
        double randomY = RandomGen.NextDouble(Level.Bottom-500, Level.Bottom);
        Vector vektori = new Vector(0, _esteenNopeus);
        PhysicsObject objekti = new PhysicsObject(objektinLeveys, objektinKorkeus);
        objekti.Shape = muoto;
        objekti.Color = vari;
        objekti.X = randomX;
        objekti.Y = randomY;
        objekti.Velocity = vektori;
        objekti.Tag = objektinTagi;
        objekti.IgnoresCollisionResponse = true;
        return objekti;
    }
    
    
    
    /// <summary>
    /// Luo peliin esteitä ottaen parametrinä kokonaisluvun
    /// joka kertoo kuinka monta estettä luodaan kerralla.
    /// Este luodaan näkyvän kentän alapuolelle satunnaiseen sijaintiin,
    /// ja sen koko myös generoidaan satunnaisesti.
    /// Esteen kuva valitaan myös satunnaisesti.
    /// </summary>
    /// <param name="montako">Kuinka monta estettä luodaan kerralla</param>
    /// <returns>Esteen tiedot</returns>
    private List<PhysicsObject> LuoEste(int montako)
    {
        for (int i = 0; i < montako; i++)
        {
            // Satunnaistetaan objektin koko
            int randomKoko = RandomGen.NextInt(30, 100);
            // Luodaan este aliohjelmalla
            PhysicsObject este = LuoFysiikkaObjekti(randomKoko, randomKoko, Shape.Circle, Color.Red, "este");
            // Valitaan satunnaisesti, onko puu vai kivi
            int mikaKuva = RandomGen.NextInt(1,3);
            if (mikaKuva == 1)
            {
                este.Image = kiviYksi;
            }
            else if (mikaKuva == 2)
            {
                este.Image = puuYksi;
            }
            kentanEsteetListana.Add(este);
            Add(este);
        }
        return kentanEsteetListana;
    }

    
    /// <summary>
    /// Luo tähden kutsumalla LuoFysiikkaObjekti aliohjelmaa.
    /// Aliohjelma lisää tähden tahdetListana listaan,
    /// jotta muualla ohjelmassa päästään käsittelemään objektien
    /// ominaisuuksia helposti
    /// </summary>
    /// <returns>Palauttaa muokatun listan tähtien tiedoista</returns>
    private List<PhysicsObject> LuoTahti(int montako)
    {
        int tahdenKoko = 30;
        for (int i = 1; i <= montako; i++)
        {
            PhysicsObject tahti = LuoFysiikkaObjekti(tahdenKoko, tahdenKoko, Shape.Star, Color.YellowGreen, "tahti");
            tahdetListana.Add(tahti);
            Add(tahti);
        }
        return tahdetListana;
    }
    
    
    /// <summary>
    /// Luo sydämen kutsumalla LuoFysiikkaObjekti aliohjelmaa.
    /// Aliohjelma lisää sydämen pingunSydametListana listaan,
    /// jotta muualla ohjelmassa päästään käsittelemään objektien
    /// ominaisuuksia helposti
    /// </summary>
    /// <returns>Palauttaa muokatun listan sydänten tiedoista</returns>
    private List<PhysicsObject> LuoSydan()
    {
        int sydamenKoko = 30;
        PhysicsObject sydan = LuoFysiikkaObjekti(sydamenKoko, sydamenKoko, Shape.Heart, Color.Red, "sydan");
        Add(sydan);
        pingunSydametListana.Add(sydan);
        return pingunSydametListana;
    }
    
    
    /// <summary>
    /// TormaysEste aliohjelmaa kutsutaan, kun pingu törmää esteeseen.
    /// Pingulta vähennetään tällöin yksi sydän, ja pelin objektien
    /// nopeutta hidastetaan. Myös objektien luonnin tahtia hidastetaan.
    /// Aliohjelma myös muuttaa Pingun ulkonäköä sydänten mukaan
    /// </summary>
    /// <param name="pelaaja">Pingu</param>
    /// <param name="esteJohonOsuttu">Este, johon pelaaja osui</param>
    private void TormaysEste(PhysicsObject pelaaja, PhysicsObject esteJohonOsuttu)
    {
        // Paljonko on minimi nopeus, että tiputetaan nopeuksia
        int esteidenNopeudenMinimi = 200;
        // Paljonko tiputetaan
        int esteidenNopeudenVahennys = 100;
        // Pingulta sydän vähemmäksi
        _pingunSydamet -= 1;
        // Sydanlaskurin muutokset
        sydanLaskuri.Value = _pingunSydamet;
        // Vähennetään esteiden nopeutta, jottei vaikeudu liikaa
        if (_esteenNopeus > esteidenNopeudenMinimi) _esteenNopeus -= esteidenNopeudenVahennys;
        // Tarkastetaan nopeudet
        Vector vektori = new Vector(0, _esteenNopeus);
        TarkastaObjektienNopeus(vektori);
        // Palautetaan ajastimen tahti alkuun
        ajastin.Interval = 1;
        esteJohonOsuttu.Destroy();
        // Vaihdetaan kuva ja tarkastetaan sydänten määrä
        SydanLaskuri();
    }


    /// <summary>
    /// Ottaa vastaan vektorin ja muuttaa objektien nopeuksia
    /// sen mukaiseksi
    /// </summary>
    /// <param name="esteidenNopeus">Nopeusvektori esteille</param>
    private void TarkastaObjektienNopeus(Vector esteidenNopeus)
    {
        // Jokaisen esteen nopeus asetetaan vektorin mukaiseksi
        // ja kentän yli menneet tuhotaan
        foreach (PhysicsObject este in kentanEsteetListana)
        {
            este.Velocity = esteidenNopeus;
            if (este.Y > Level.Top) este.Destroy();
        }
        
        // Jokaisen sydamen nopeus asetetaan vektorin mukaiseksi
        // Ei tarvetta tuhota, koska näitä tuhotaan muullonkin.
        foreach (PhysicsObject sydan in pingunSydametListana)
        {
            sydan.Velocity = esteidenNopeus;
        }

        foreach (PhysicsObject tahti in tahdetListana)
        {
            tahti.Velocity = esteidenNopeus;
        }
    }
    
    
    /// <summary>
    /// Kutsutaan, kun pingu törmää sukseen.
    /// Pingulle lisätään sydän, jos sydämiä on alle kolme.
    /// Pingun kuvaa muutetaan sydänten mukaan.
    /// </summary>
    /// <param name="pelaaja"></param>
    /// <param name="osuttuSydan"></param>
    private void TormaysSydan(PhysicsObject pelaaja, PhysicsObject osuttuSydan)
    {
        if (_pingunSydamet < SydantenMaksimi) _pingunSydamet += 1;
        sydanLaskuri.Value = _pingunSydamet;
        //Console.WriteLine(sydamet);
        osuttuSydan.Destroy();
        SydanLaskuri();
    }
    
    
    /// <summary>
    /// Tuhoaa törmäyksessä tähden ja lisää laskurin arvoa
    /// </summary>
    /// <param name="pelaaja">Pingu</param>
    /// <param name="osuttuTahti">Tähti johon osuttu</param>
    private void TormaysTahti(PhysicsObject pelaaja, PhysicsObject osuttuTahti)
    {
        osuttuTahti.Destroy();
        tahtiLaskuri.AddValue(1);
    }


    /// <summary>
    /// Asettaa pingun kuvan riippuen pingun sydanten määrästä.
    /// Jos sydämiä on nolla, peli päättyy.
    /// Jos pingulla sydämiä ainakin maksimi määrä, tuhotaan ylimääräiset sydämet
    /// </summary>
    private void SydanLaskuri()
    {
        if (_pingunSydamet >= SydantenMaksimi)
        {
            pingu.Image = pingunKuvaKolme;
            // Kun sydämia on kolme, tuhotaan loput sydämet
            foreach (PhysicsObject sydan in pingunSydametListana)
            {
                if (sydan == null) break;
                sydan.Destroy();
            }
        }
        else if (_pingunSydamet == 2) pingu.Image = pingunKuvaKaksi;
        else if (_pingunSydamet == 1) pingu.Image = pingunKuvaYksi;
        else if (_pingunSydamet == 0)
        {
            SydametNolla();
        }
    }


    /// <summary>
    /// Hoitaa toimet, kun Pingun sydämet ovat nollassa.
    /// Objektien liike pysäytetään.
    /// TODO ~ Tee hieno valikko, et alottaako alusta vai ei
    /// </summary>
    private void SydametNolla()
    {
        aikaLaskuri.Stop();
        StopAll();
        foreach (PhysicsObject sydan in pingunSydametListana)
        {
            sydan.LinearDamping = 100;
            sydan.Velocity = Vector.Zero;
        }
        foreach (PhysicsObject este in kentanEsteetListana)
        {
            este.LinearDamping = 100;
            este.Velocity = Vector.Zero;
        }
        pingu.LinearDamping = 100;
        pingu.Image = pingunKuvaNolla;
        TulostaTiedot();
        Keyboard.Listen(Key.Enter, ButtonState.Pressed, Restart, "Aloita peli uudestaan");
    }
    
    
    /// <summary>
    /// Pelin lopuksi tulostetaan joitakin tilastoja,
    /// kuten syntyneiden esteiden ja sydänten lukumäärä,
    /// sekä kuljetun matkan viemä aika
    /// </summary>
    private void TulostaTiedot()
    {
        // Tätä kutsutaan sitten, kun sydamet on nollassa. 
        MessageDisplay.Add($"Pingun after ski jäi nyt välistä");
        MessageDisplay.Add($"--------------------------------");
        MessageDisplay.Add($"Keräsit {tahtiLaskuri.Value} tähteä!");
        MessageDisplay.Add($"Kentällä oli yhteensä {kentanEsteetListana.Count} estettä");
        MessageDisplay.Add($"Sydämiä oli yhteensä {pingunSydametListana.Count}");
        MessageDisplay.Add($"Aikasi oli {aikaLaskuri.CurrentTime} sekuntia");
        MessageDisplay.Add($"--------------------------------");
        MessageDisplay.Add("Paina enter aloittaaksesi uudestaan!");
    }

    
    /// <summary>
    /// Restart aliohjelma hoitaa toimet, kun pelaaja päättää
    /// aloittaa pelin uudestaan. Se poistaa aiemmat objektit
    /// ja kutsuu Begin aliohjelmaa aloittaen pelin alusta.
    /// </summary>
    private void Restart()
    {
        ClearAll();
        _pingunSydamet = SydantenMaksimi;
        Begin();
    }
}