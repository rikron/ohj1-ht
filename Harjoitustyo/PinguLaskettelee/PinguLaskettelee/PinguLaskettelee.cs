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
///    
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
    // On ikävää, jos esteen muoto onkin erilainen, kun kuvasta päättelisi
    private Shape kiviMuoto = Shape.FromImage(kiviYksi);
    private Shape puuMuoto = Shape.FromImage(puuYksi);
    private Shape pinguMuoto = Shape.FromImage(pingunKuvaKolme);
    
    /// <summary>
    /// TODO ~ NÄMÄ PITÄÄ MUISTAA MUUTTAA VAKIOIKSI, OSA SIIRRETTÄVÄ PELIN SISÄLLE
    /// </summary>
    private int sydamet = 3;
    private int esteenNopeus = 300;
    private int esteidenMaara = 0;
    private int tuhottu = 0;

    
    private double interval = 1;
    private double kuljettuMatka = 0;
    
    
    private PhysicsObject pingu;
    private PhysicsObject kivi;
    private PhysicsObject suksi;

    private Timer ajastin;
    
    private PhysicsObject[] este = new PhysicsObject[1000];
 
    
    private double[] esteTiedot = new double[3];
    private double[] pinguTiedot = new double[3];
    
    
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
        
        pingu = Pingu();
        
        // Camera.FollowedObject = pingu; TODO ~ Tämä on ihan hieno!
        
        LisaaLaskuri();
        AsetaOhjaimet();
        Update();
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
    
    private void AsetaOhjaimet()
    {
        Keyboard.Listen(Key.A, ButtonState.Down, LiikutaPingua, "Liikuttaa pingua vasemmalle", pingu, new Vector(-esteenNopeus-100, 0), -1.0);
        Keyboard.Listen(Key.D, ButtonState.Down, LiikutaPingua, "Liikuttaa pingua oikealle", pingu, new Vector(esteenNopeus+100, 0), 1.0);
        
    }


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

    
    private void Laskin()
    {
        int random = RandomGen.NextInt(0, 100);
        esteenNopeus += 10;
        if (sydamet == 0) esteenNopeus = 0;
        Vector vektori = new Vector(0, esteenNopeus);
        for (int i = 0; i < esteidenMaara; i++)
        {
            este[i].Velocity = vektori;
            if (este[i].Y > 0) este[i].Destroy();
        }
        if (ajastin.Interval > 0.5) ajastin.Interval -= 0.01;
        Console.WriteLine("AJASTIN "+ajastin.Interval);
        Console.WriteLine("interval on "+interval);
        kuljettuMatka += interval;
        //Console.WriteLine(este[3].Y);
        if (random < 75 && sydamet > 0)
        {
            int random2 = RandomGen.NextInt(3, 5);
            este = LuoEste(random2, 100);
            esteTiedot[0]++;
        }
        
        
        

        if (random < 25 && sydamet < 3 && sydamet > 0)
        {
            suksi = LuoSuksi();
        }
        //Console.WriteLine("kuljettu matka on "+kuljettuMatka);
        //if (kuljettuMatka>10) sydamet = 0;
    }

    
    
    private PhysicsObject Pingu()
    {
        pingu = new PhysicsObject(50, 50);
        pingu.Shape = Shape.Circle;
        pingu.Color = Color.Red;
        pingu.X = 0;
        pingu.Y = 0;
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
    
    
    private PhysicsObject[] LuoEste(int montako, double koko)
    {
        Vector vektori = new Vector(0, esteenNopeus);
        for (int i = 0; i < montako; i++)
        {
            int randomX = RandomGen.NextInt(-450, 450);
            int randomY = RandomGen.NextInt(-850, -450);
            PhysicsObject kivi = new PhysicsObject(koko, koko);
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


    private PhysicsObject LuoSuksi()
    {
        int randomX = RandomGen.NextInt(-450, 450);
        int randomY = RandomGen.NextInt(-850, -450);
        Vector vektori = new Vector(0, esteenNopeus);
        PhysicsObject suksi = new PhysicsObject(100, 100);
        suksi.Shape = Shape.Circle;
        suksi.Color = Color.Red;
        suksi.X = randomX;
        suksi.Y = randomY;
        suksi.Velocity = vektori;
        suksi.Tag = "suksi";
        suksi.IgnoresCollisionResponse = true;
        //este.Image = kiviYksi;
        Add(suksi);
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
        esteenNopeus -= 100;
        Vector vektori = new Vector(0, esteenNopeus);
        for (int i = 0; i < esteidenMaara; i++)
        {
            try
            {
                este[i].Velocity = vektori;
            }
            catch
            {
                este[esteidenMaara].Velocity = vektori;
            }
        }
        ajastin.Interval = 1;
        Console.WriteLine(sydamet);
        esteet.Destroy();
        if (sydamet == 2) pingu.Image = pingunKuvaKaksi;
        if (sydamet == 1) pingu.Image = pingunKuvaYksi;
        if (sydamet == 0)
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
            // TODO ~ Tähän lisätään esteidenTiedot kutsu
        }
    }
    
    
    private void TormaysSuksi(PhysicsObject pingu, PhysicsObject suksi)
    {
        if (sydamet <= 3) sydamet += 1;
        sydanLaskuri.Value = sydamet;
        Console.WriteLine(sydamet);
        //suksi.Destroy();
        if (sydamet == 3) pingu.Image = pingunKuvaKolme;
        if (sydamet == 2) pingu.Image = pingunKuvaKaksi;
        if (sydamet == 1) pingu.Image = pingunKuvaYksi;
        if (sydamet == 0)
        {
            StopAll();
            suksi.Velocity = Vector.Zero;
            pingu.LinearDamping = 100;
            pingu.Image = pingunKuvaNolla;
            MessageDisplay.Add($"Pingun after ski jäi nyt välistä");
            // TulostaTiedot(esteTiedot, pinguTiedot)
            //Console.WriteLine($"Esteiden määrä oli {esteidenMaara}");
        }
    }


    private void TulostaTiedot(double[] esteTiedot, double[] pinguTiedot)
    {
        // TODO ~~ pitää lisätä toiminnot, jotta tiedot tulostetaan pelin lopussa
        // Tätä kutsutaan sitten, kun sydamet on nollassa. 
        // Lisäisikö aloita uudestaan napin tänne?
    }
}