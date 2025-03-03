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
/// @version 24.02.2025
/// <summary>
/// Pingu Laskettelee! on yksinpelattava scroller peli, jossa
/// pelaaja välttelee satunnaisia esteitä ja pyrkii pääsemään maaliin
/// vahingoittumatta.
/// </summary>
public class PinguLaskettelee : PhysicsGame
{
    // Pelin etenemisen nopeutta määrää laskuri
    // Nämä rajoittavat laskurin 
    const int laskurinMaksimi = 3;
    const int laskurinMinimi = 0;
        
    
    /// Pingun kuvat eri tilanteissa
    private static readonly Image pingunKuvaKolme = LoadImage("pinguKolme");
    private static readonly Image pingunKuvaKaksi = LoadImage("pinguKaksi");
    private static readonly Image pingunKuvaYksi = LoadImage("pinguYksi");
    private static readonly Image pingunKuvaNolla = LoadImage("pinguNolla");
    
    
    /// Kuvat esteistä
    private static readonly Image kiviYksi = LoadImage("kivi");
    private static readonly Image puuYksi = LoadImage("puu");
    
    
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
    /// TODO ~ NÄMÄ PITÄÄ MUISTAA MUUTTAA VAKIOIKSI, OSA SIIRRETTÄVÄ PELIN SISÄLLE
    /// </summary>
    private int sydamet;
    private int esteenNopeus;
    private int esteidenMaara;
    private int suksienMaara;
    private int tuhottu;
    
    private double interval;
    private double kuljettuMatka;
    
    private PhysicsObject pingu;
    private PhysicsObject kivi;
    private PhysicsObject[] suksi;
    private PhysicsObject[] este;
    
    private Timer ajastin;
    
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
        Level.CreateBorders();
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
        sydamet = 3;
        esteenNopeus = 300;
        esteidenMaara = 0;
        suksienMaara = 0; 
        tuhottu = 0;
    
        interval = 1;
        kuljettuMatka = 0;
        
        suksi = new PhysicsObject[1000];
        este = new PhysicsObject[1000];
        
        esteTiedot = new double[3];
        pinguTiedot = new double[3];
    }
    
    
    
    private void LisaaLaskuri()
    {
        sydanLaskuri = LuoLaskuri();
    }

    
    private IntMeter LuoLaskuri()
    {
        IntMeter laskuri = new IntMeter(3);
        laskuri.MinValue = laskurinMinimi;
        laskuri.MaxValue = laskurinMaksimi;
        
        Label naytto = new Label();
        naytto.BindTo(laskuri);
        naytto.X = 0;
        naytto.Y = Level.Top - 50;
        naytto.TextColor = Color.Black;
        naytto.BorderColor = Color.White;
        naytto.Color = Color.White;
        Add(naytto);
        
        return laskuri;
    }
    
    
    /// <summary>
    /// Asettaa Pingun ohjaimet.
    /// Näppäimistön A liikuttaa vasemmalle, D liikuttaa oikealle.
    /// </summary>
    private void AsetaOhjaimet()
    {
        Keyboard.Listen(Key.A, ButtonState.Down, LiikutaPingua, "Liikuttaa pingua vasemmalle", pingu, new Vector(-esteenNopeus-300, 0), -1.0);
        Keyboard.Listen(Key.D, ButtonState.Down, LiikutaPingua, "Liikuttaa pingua oikealle", pingu, new Vector(esteenNopeus+300, 0), 1.0);
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
    
    
    private void Update()
    {
        ajastin = new Timer();
        ajastin.Interval = interval;
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
        int random = RandomGen.NextInt(0, 100);
        esteenNopeus += 10;
        if (sydamet == 0) esteenNopeus = 0;
        Vector vektori = new Vector(0, esteenNopeus);
        
        for (int i = 0; i < esteidenMaara; i++)
        {
            este[i].Velocity = vektori;
            if (este[i].Y > 500) este[i].Destroy();
            try
            {
                suksi[i].Velocity = vektori;
            }
            catch
            {
                este[i].Velocity = vektori;
            }
        }
        
        if (ajastin.Interval > 0.5) ajastin.Interval -= 0.01;
        //Console.WriteLine("AJASTIN "+ajastin.Interval);
        //Console.WriteLine("interval on "+interval);
        kuljettuMatka += interval;
        //Console.WriteLine(este[3].Y);
        
        if (random < 75 && sydamet > 0)
        {
            int random2 = RandomGen.NextInt(3, 5);
            este = LuoEste(random2);
            esteTiedot[0]++;
        }
        
        if (random < 75 && sydamet < 3 && sydamet > 0)
        {
            suksi = LuoSuksi();
        }
        //Console.WriteLine("kuljettu matka on "+kuljettuMatka);
        //if (kuljettuMatka>10) sydamet = 0;
        if (sydamet == 3)
        {
            for (int i = 0; i < suksienMaara; i++)
            {
                suksi[i].Destroy();
            }
        }
    }

    
    
    private PhysicsObject Pingu()
    {
        pingu = new PhysicsObject(50, 50);
        pingu.Shape = Shape.Circle;
        pingu.Color = Color.Red;
        pingu.X = 0;
        pingu.Y = 200;
        pingu.Image = pingunKuvaKolme;
        pingu.RotateImage = false;
        pingu.IgnoresCollisionResponse = false;
        pingu.MaxAngularVelocity = 2;
        pingu.LinearDamping = 0.998;
        //AddCollisionHandler(pingu, Tormays);
        AddCollisionHandler(pingu, "este", TormaysEste);
        AddCollisionHandler(pingu, "suksi", TormaysSuksi);
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
        Vector vektori = new Vector(0, esteenNopeus);
        for (int i = 0; i < montako; i++)
        {
            int randomX = RandomGen.NextInt(-450, 450);
            int randomY = RandomGen.NextInt(-850, -450);
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
            este[esteidenMaara] = kivi;
            esteidenMaara++;
            Add(kivi);
        }
        return este;
    }

    
    /// <summary>
    /// Luo suksen
    /// TODO ~ Grafiikat ois kivat
    /// </summary>
    /// <returns></returns>
    private PhysicsObject[] LuoSuksi()
    {
        int randomX = RandomGen.NextInt(-450, 450);
        int randomY = RandomGen.NextInt(-850, -450);
        Vector vektori = new Vector(0, esteenNopeus);
        PhysicsObject objekti = new PhysicsObject(30, 30);
        objekti.Shape = Shape.Circle;
        objekti.Color = Color.Red;
        objekti.X = randomX;
        objekti.Y = randomY;
        objekti.Velocity = vektori;
        objekti.Tag = "suksi";
        objekti.IgnoresCollisionResponse = true;
        //este.Image = kiviYksi;
        Add(objekti);
        suksi[suksienMaara] = objekti;
        suksienMaara++;
        //esteidenMaara++;
        return suksi;
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
        sydamet -= 1;
        sydanLaskuri.Value = sydamet;
        if (esteenNopeus > 200) esteenNopeus -= 100;
        Vector vektori = new Vector(0, esteenNopeus);
        for (int i = 0; i < esteidenMaara; i++)
        {
            este[i].Velocity = vektori;
            if (suksi[i] != null) suksi[i].Velocity = vektori;
        }
        ajastin.Interval = 1;
        Console.WriteLine(sydamet);
        esteet.Destroy();
        if (sydamet == 2) pingu.Image = pingunKuvaKaksi;
        if (sydamet == 1) pingu.Image = pingunKuvaYksi;
        if (sydamet == 0)
        {
            SydametNolla();
        }
    }
    
    
    /// <summary>
    /// Kutsutaan, kun pingu törmää sukseen.
    /// Pingulle lisätään sydän, jos sydämiä on alle kolme.
    /// Pingun kuvaa muutetaan sydänten mukaan.
    /// </summary>
    /// <param name="pelaaja"></param>
    /// <param name="sukset"></param>
    private void TormaysSuksi(PhysicsObject pelaaja, PhysicsObject sukset)
    {
        if (sydamet < 3) sydamet += 1;
        sydanLaskuri.Value = sydamet;
        //Console.WriteLine(sydamet);
        sukset.Destroy();
        if (sydamet == 3) pingu.Image = pingunKuvaKolme;
        if (sydamet == 2) pingu.Image = pingunKuvaKaksi;
        if (sydamet == 1) pingu.Image = pingunKuvaYksi;
        if (sydamet == 0)
        {
            SydametNolla();
        }
    }

    
    private void TormaysTahti(PhysicsObject pingu, PhysicsObject tahti)
    {
        //tahtiMaara += 1;
        //tahti.Destroy();
    }
    

    private void TulostaTiedot(double[] esteTiedot, double[] pinguTiedot)
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
        for (int i = 0; i < esteidenMaara; i++)
        {
            este[i].Velocity = Vector.Zero;
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
        sydamet = 3;
        Begin();
    }
}