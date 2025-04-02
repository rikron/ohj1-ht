using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using SixLabors.ImageSharp.Processing.Processors.Filters;

// TODO ~ Poista ylimääräset using direktiivit

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
    
    /// <summary>
    /// Pingun kuvia eri sydänmäärillä
    /// </summary>
    private static readonly Image pingunKuvaKolme = LoadImage("pinguKolme.png");
    private static readonly Image pingunKuvaKaksi = LoadImage("pinguKaksi.png");
    private static readonly Image pingunKuvaYksi = LoadImage("pinguYksi.png");
    private static readonly Image pingunKuvaNolla = LoadImage("pinguNolla.png");
    
    /// Kiven kuva
    private static readonly Image kiviYksi = LoadImage("kivi.png");
    /// Puun kuva
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
    private int _esteidenLukumaara;
    private int _sydantenLukumaara;
    //private int tuhottu;
    
    private double _ajastimenIntervalli;
    //private double kuljettuMatka;
    
    private PhysicsObject pingu;
    private PhysicsObject kivi;
    private PhysicsObject[] pingunSydamet;
    private PhysicsObject[] kentanEsteet;
    
    private Timer ajastin;
    
    /// <summary>
    /// 
    /// </summary>
    private double[] esteTiedot;
    
    private double[] pinguTiedot;
    
    
    public IntMeter sydanLaskuri;
    

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
        IsFullScreen = false;
        Vector kentanKoko = new Vector(1920, 1080);
        Vector naytonKoko = new Vector();
        Console.WriteLine(Device.DisplayResolution.Width + " " + Device.DisplayResolution.Height);
        Level.Size = kentanKoko;
        Level.CreateBorders(0, false);
        Level.BackgroundColor = Color.White;
        // IsFullScreen = true; TODO ~ Pitäisikö tehdä fullscreen peli??
        LuoMuuttujat();
        pingu = Pingu();
        
        // Camera.FollowedObject = pingu; TODO ~ Tämä on ihan hieno!
        
        LisaaLaskuri();
        AsetaOhjaimet();
        Update();
    }
    
    
    /// <summary>
    /// LuoMuuttujat aliohjelma hoitaa tarvittavien muuttujien arvojen asettamisen
    /// pelin alussa. 
    /// </summary>
    private void LuoMuuttujat()
    {
        _pingunSydamet = 3;
        _esteenNopeus = 300;
        _esteidenLukumaara = 0;
        _sydantenLukumaara = 0; 
        //tuhottu = 0;
    
        _ajastimenIntervalli = 1;
        //kuljettuMatka = 0;
        
        pingunSydamet = new PhysicsObject[1000];
        kentanEsteet = new PhysicsObject[1000];
        
        esteTiedot = new double[3];
        pinguTiedot = new double[3];
    }
    
    
    /// <summary>
    /// TODO Lisätään tähän matkalaskurin sun muut hilavitkuttimet
    /// </summary>
    private void LisaaLaskuri()
    {
        sydanLaskuri = LuoLaskuri();
    }

    
    /// <summary>
    /// Aliohjelma luo sydänlaskurin, ja lisää sen ruudulle
    /// </summary>
    /// <returns>Palauttaa IntMeter laskurin</returns>
    private IntMeter LuoLaskuri()
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
    /// Asettaa Pingun ohjaimet.
    /// Näppäimistön A liikuttaa vasemmalle, D liikuttaa oikealle.
    /// </summary>
    private void AsetaOhjaimet()
    {
        int pingunNopeus = 700;
        Keyboard.Listen(Key.A, ButtonState.Down, LiikutaPingua, "Liikuttaa pingua vasemmalle", pingu, new Vector(-pingunNopeus, 0), -1.0);
        Keyboard.Listen(Key.D, ButtonState.Down, LiikutaPingua, "Liikuttaa pingua oikealle", pingu, new Vector(pingunNopeus, 0), 1.0);
    }


    /// <summary>
    /// TODO ~ Poista kulmanopeus parametri tai hyödynnä sitä!
    /// Suorittaa Pingun liikuttamisen näppäimistön painalluksissa. 
    /// </summary>
    /// <param name="pelaaja">Mitä liikutetaan, eli pingu</param>
    /// <param name="suunta">Mihin suuntaan liikutetaan</param>
    /// <param name="kulmanopeus">(EI KÄYTÖSSÄ)</param>
    private void LiikutaPingua(PhysicsObject pelaaja, Vector suunta, double kulmanopeus)
    {
        pingu.Push(suunta);
    }
    
    
    /// <summary>
    /// Luodaan tietyn väliajoin päivittävä laskuri,
    /// joka kutsuu Laskin aliohjelmaa, intervallin välein
    /// </summary>
    private void Update()
    {
        ajastin = new Timer();
        ajastin.Interval = _ajastimenIntervalli;
        ajastin.Timeout += Laskin;
        ajastin.Start();
    }

    
    /// <summary>
    /// Laskin kertoo, mitä tehdään aina ajastimen mukaisen intervallin aikana
    /// TODO ~ Tätä voisi jaotella aliohjelmiin
    /// TODO Vois myös noita numeroita vaihtaa muuttujiin
    /// </summary>
    private void Laskin()
    {
        // Nämä kokonaisluvut luovat satunnaisuutta esteiden syntymiseen
        int satunnainenArvo = RandomGen.NextInt(0, 100);
        int satunnaisuudenProsentti = 99;

        int esteenNopeudenLisays = 10;
        
        _esteenNopeus += esteenNopeudenLisays;
        
        if (_pingunSydamet == 0) _esteenNopeus = 0;
        Vector vektori = new Vector(0, _esteenNopeus);
        
        for (int i = 0; i < _esteidenLukumaara; i++)
        {
            kentanEsteet[i].Velocity = vektori;
            if (kentanEsteet[i].Y > Level.Top) kentanEsteet[i].Destroy();
            try
            {
                pingunSydamet[i].Velocity = vektori;
            }
            catch
            {
                kentanEsteet[i].Velocity = vektori;
            }
        }
        
        if (ajastin.Interval > 0.5) ajastin.Interval -= 0.01;
        //Console.WriteLine("AJASTIN "+ajastin.Interval);
        //Console.WriteLine("interval on "+interval);
        //kuljettuMatka += _ajastimenIntervalli;
        //Console.WriteLine(este[3].Y);
        
        // Tässä luodaan esteitä, jos satunnainnaisest luotu kokonaisluku on alle raja-arvon
        // ja sydämiä on enemmän kuin 0
        if (satunnainenArvo < satunnaisuudenProsentti && _pingunSydamet > 0)
        {
            int random2 = RandomGen.NextInt(3, 5);
            kentanEsteet = LuoEste(random2);
            esteTiedot[0]++;
        }
        
        // Tässä luodaan suksia, jos satunnainnaisest luotu kokonaisluku on alle raja-arvon
        // ja sydämiä on enemmän kuin 0
        if (satunnainenArvo < satunnaisuudenProsentti && _pingunSydamet is < 3 and > 0)
        {
            pingunSydamet = LuoSydän();
        }
        //Console.WriteLine("kuljettu matka on "+kuljettuMatka);
        //if (kuljettuMatka>10) sydamet = 0;
        /*if (sydamet == 3)
        {
            for (int i = 0; i < suksienMaara; i++)
            {
                suksi[i].Destroy();
            }
        }*/
    }

    
    /// <summary>
    /// Pelaajan liikuttama fysiikkaolio Pingu
    /// </summary>
    /// <returns>Palauttaa PhysicObject pingun.</returns>
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
        pingu.RotateImage = false;
        pingu.IgnoresCollisionResponse = false;
        //pingu.MaxAngularVelocity = 2;
        pingu.LinearDamping = pingunLinearDamping;
        //AddCollisionHandler(pingu, Tormays);
        AddCollisionHandler(pingu, "este", TormaysEste);
        AddCollisionHandler(pingu, "suksi", TormaysSuksi);
        //AddCollisionHandler(Level., "pingu" );
        Add(pingu);
        return pingu;
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
    private PhysicsObject[] LuoEste(int montako)
    {
        Vector vektori = new Vector(0, _esteenNopeus);
        for (int i = 0; i < montako; i++)
        {
            double randomX = RandomGen.NextDouble(Level.Left, Level.Right);
            double randomY = RandomGen.NextDouble(Level.Bottom-500, Level.Bottom);
            int randomKoko = RandomGen.NextInt(30, 100);
            kivi = new PhysicsObject(randomKoko, randomKoko);
            kivi.Color = Color.Red;
            kivi.X = randomX;
            kivi.Y = randomY;
            kivi.Velocity = vektori;
            kivi.Tag = "este";
            kivi.IgnoresCollisionResponse = true;
            int mikaKuva = RandomGen.NextInt(1,3);
            if (mikaKuva == 1)
            {
                kivi.Image = kiviYksi;
                kivi.Shape = Shape.Circle;
            }
            else if (mikaKuva == 2)
            {
                kivi.Image = puuYksi;
                kivi.Shape = Shape.Circle;
            }
            kentanEsteet[_esteidenLukumaara] = kivi;
            _esteidenLukumaara++;
            Add(kivi);
        }
        return kentanEsteet;
    }

    
    /// <summary>
    /// Luo suksen
    /// TODO ~ Grafiikat ois kivat
    /// </summary>
    /// <returns></returns>
    private PhysicsObject[] LuoSydän()
    {
        double randomX = RandomGen.NextDouble(Level.Left, Level.Right);
        double randomY = RandomGen.NextDouble(Level.Bottom-500, Level.Bottom);
        Vector vektori = new Vector(0, _esteenNopeus);
        PhysicsObject objekti = new PhysicsObject(30, 30);
        objekti.Shape = Shape.Heart;
        objekti.Color = Color.Red;
        objekti.X = randomX;
        objekti.Y = randomY;
        objekti.Velocity = vektori;
        objekti.Tag = "suksi";
        objekti.IgnoresCollisionResponse = true;
        //este.Image = kiviYksi;
        Add(objekti);
        pingunSydamet[_sydantenLukumaara] = objekti;
        _sydantenLukumaara++;
        //esteidenMaara++;
        return pingunSydamet;
    }
    
    
    /// <summary>
    /// TormaysEste aliohjelmaa kutsutaan, kun pingu törmää esteeseen.
    /// Pingulta vähennetään tällöin yksi sydän, ja pelin objektien
    /// nopeutta hidastetaan. Myös objektien luonnin tahtia hidastetaan.
    /// Aliohjelma myös muuttaa Pingun ulkonäköä sydänten mukaan
    /// </summary>
    /// <param name="pelaaja"></param>
    /// <param name="esteet"></param>
    private void TormaysEste(PhysicsObject pelaaja, PhysicsObject esteet)
    {
        _pingunSydamet -= 1;
        sydanLaskuri.Value = _pingunSydamet;
        if (_esteenNopeus > 200) _esteenNopeus -= 100;
        Vector vektori = new Vector(0, _esteenNopeus);
        for (int i = 0; i < _esteidenLukumaara; i++)
        {
            kentanEsteet[i].Velocity = vektori;
            if (pingunSydamet[i] != null) pingunSydamet[i].Velocity = vektori;
        }
        ajastin.Interval = 1;
        Console.WriteLine(_pingunSydamet);
        esteet.Destroy();
        SydanLaskuri();
    }
    
    
    /// <summary>
    /// Kutsutaan, kun pingu törmää sukseen.
    /// Pingulle lisätään sydän, jos sydämiä on alle kolme.
    /// Pingun kuvaa muutetaan sydänten mukaan.
    /// </summary>
    /// <param name="pelaaja"></param>
    /// <param name="osuttuSydan"></param>
    private void TormaysSuksi(PhysicsObject pelaaja, PhysicsObject osuttuSydan)
    {
        if (_pingunSydamet < SydantenMaksimi) _pingunSydamet += 1;
        sydanLaskuri.Value = _pingunSydamet;
        //Console.WriteLine(sydamet);
        osuttuSydan.Destroy();
        SydanLaskuri();
    }


    /// <summary>
    /// Asettaa pingun kuvan riippuen sydanten määrästä
    /// </summary>
    private void SydanLaskuri()
    {
        if (_pingunSydamet >= SydantenMaksimi)
        {
            pingu.Image = pingunKuvaKolme;
            // Kun sydämia on kolme, tuhotaan loput sydämet
            foreach (PhysicsObject sydan in pingunSydamet)
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

    
    private void TormaysTahti(PhysicsObject pelaaja, PhysicsObject tahti)
    {
        //TODO Lisää tähtisysteemit
        //tahtiMaara += 1;
        //tahti.Destroy();
    }
    

    private void TulostaTiedot(double[] esteidenTiedot, double[] pingunTiedot)
    {
        // TODO ~~ pitää lisätä toiminnot, jotta tiedot tulostetaan pelin lopussa
        // Tätä kutsutaan sitten, kun sydamet on nollassa. 
        // Lisäisikö aloita uudestaan napin tänne?
    }


    /// <summary>
    /// Hoitaa toimet, kun Pingun sydämet ovat nollassa.
    /// Objektien liike pysäytetään.
    /// TODO ~ Tee hieno valikko, et alottaako alusta vai ei
    /// </summary>
    private void SydametNolla()
    {
        StopAll();
        for (int i = 0; i < _esteidenLukumaara; i++)
        {
            kentanEsteet[i].Velocity = Vector.Zero;
            StopAll();
        }
        pingu.LinearDamping = 100;
        pingu.Image = pingunKuvaNolla;
        MessageDisplay.Add($"Pingun after ski jäi nyt välistä");
        MessageDisplay.Add("Paina enter aloittaaksesi uudestaan!");
        Keyboard.Listen(Key.Enter, ButtonState.Pressed, Restart, "Aloita peli uudestaan");
        // TODO ~ Tähän lisätään esteidenTiedot kutsu
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