using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using SixLabors.ImageSharp.Processing.Processors.Filters;

namespace PinguLaskettelee;

/// @author Riku Rönkä
/// @version 24.02.2025
/// <summary>
///    
/// </summary>
public class PinguLaskettelee : PhysicsGame
{
    private int sydamet = 3;
    private int esteenNopeus = 300;
    private int esteidenMaara = 0;

    private double interval = 1;
    private double kuljettuMatka = 0;
    
    private PhysicsObject pingu;
    private PhysicsObject kivi;
    private PhysicsObject suksi;

    private double[] esteTiedot = new double[3];
    private double[] pinguTiedot = new double[3];

    public IntMeter sydanLaskuri;
    
    /// <summary>
    /// Pingun kuvat eri tilanteissa
    /// </summary>
    private static readonly Image pingunKuvaKolme = LoadImage("pinguKolme");
    private static readonly Image pingunKuvaKaksi = LoadImage("pinguKaksi");
    private static readonly Image pingunKuvaYksi = LoadImage("pinguYksi");
    private static readonly Image pingunKuvaNolla = LoadImage("pinguNolla");
    
    
    /// <summary>
    /// Kuvat esteistä
    /// </summary>
    private static readonly Image kiviYksi = LoadImage("Kivi");
    
    
    public override void Begin()
    {
        LuoKentta();
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }

    
    private void LuoKentta()
    {
        Level.CreateBorders();
        Level.BackgroundColor = Color.White;
        pingu = Pingu();
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
        laskuri.MaxValue = 3;
        laskuri.MinValue = 0;
        
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


    private void LiikutaPingua(PhysicsObject pingu, Vector suunta, double kulmanopeus)
    {
        pingu.Push(suunta);
    }
    
    
    private void Update()
    {
        Timer ajastin = new Timer();
        ajastin.Interval = interval;
        ajastin.Timeout += Laskin;
        ajastin.Start();
    }

    
    private void Laskin()
    {
        int random = RandomGen.NextInt(0, 100);
        esteenNopeus += 10;
        interval += 0.01;
        kuljettuMatka += interval;
        
        if (random < 75 && sydamet > 0)
        {
            int random2 = RandomGen.NextInt(3, 5);
            for (int i = 0; i < random2; i++)
            {
                kivi = LuoEste();
                esteTiedot[0]++;
                if (kivi.Y > 0) kivi.Destroy();
                // Console.WriteLine(esteTiedot[0]);
            }
        }

        if (random < 25 && sydamet < 3 && sydamet > 0)
        {
            suksi = LuoSuksi();
        }
        
        if (kuljettuMatka>100) StopAll();
    }

    
    
    private PhysicsObject Pingu()
    {
        pingu = new PhysicsObject(50, 50);
        pingu.Shape = Shape.Circle;
        pingu.Color = Color.Red;
        pingu.X = 0;
        pingu.Y = 0;
        pingu.Image = pingunKuvaKolme;
        pingu.RotateImage = true;
        pingu.IgnoresCollisionResponse = true;
        pingu.MaxAngularVelocity = 2;
        pingu.LinearDamping = 0.998;
        //AddCollisionHandler(pingu, Tormays);
        AddCollisionHandler(pingu, "este", TormaysEste);
        AddCollisionHandler(pingu, "suksi", TormaysSuksi);
        Add(pingu);
        return pingu;
    }
    
    
    private PhysicsObject LuoEste()
    {
        int randomX = RandomGen.NextInt(-450, 450);
        int randomY = RandomGen.NextInt(-850, -450);
        Vector vektori = new Vector(0, esteenNopeus);
        PhysicsObject este = new PhysicsObject(100, 100);
        este.Shape = Shape.Circle;
        este.Color = Color.Red;
        este.X = randomX;
        este.Y = randomY;
        este.Velocity = vektori;
        este.Tag = "este";
        este.IgnoresCollisionResponse = true;
        este.Image = kiviYksi;
        Add(este);
        esteidenMaara++;
        return este;
    }


    private PhysicsObject LuoSuksi()
    {
        int randomX = RandomGen.NextInt(-450, 450);
        int randomY = RandomGen.NextInt(-850, -450);
        Vector vektori = new Vector(0, esteenNopeus);
        PhysicsObject este = new PhysicsObject(100, 100);
        este.Shape = Shape.Circle;
        este.Color = Color.Red;
        este.X = randomX;
        este.Y = randomY;
        este.Velocity = vektori;
        este.Tag = "suksi";
        este.IgnoresCollisionResponse = true;
        //este.Image = kiviYksi;
        Add(este);
        esteidenMaara++;
        return este;
    }
    
    /// <summary>
    /// TormaysEste aliohjelmaa kutsutaan, kun pingu törmää esteeseen.
    /// Pingulta vähennetään tällöin yksi sydän, ja pelin objektien
    /// nopeutta hidastetaan. Myös objektien luonnin tahtia hidastetaan.
    /// Aliohjelma myös muuttaa Pingun ulkonäköä sydänten mukaan
    /// </summary>
    /// <param name="pingu"></param>
    /// <param name="este"></param>
    private void TormaysEste(PhysicsObject pingu, PhysicsObject este)
    {
        sydamet -= 1;
        sydanLaskuri.Value = sydamet;
        esteenNopeus -= 100;
        Vector vektori = new Vector(0, esteenNopeus);
        este.Velocity = vektori;
        interval -= 0.5;
        Console.WriteLine(sydamet);
        este.Destroy();
        if (sydamet == 2) pingu.Image = pingunKuvaKaksi;
        if (sydamet == 1) pingu.Image = pingunKuvaYksi;
        if (sydamet == 0)
        {
            StopAll();
            este.Velocity = Vector.Zero;
            pingu.LinearDamping = 100;
            pingu.Image = pingunKuvaNolla;
            MessageDisplay.Add($"Pingun after ski jäi nyt välistä");
            // TulostaTiedot(esteTiedot, pinguTiedot)
            //Console.WriteLine($"Esteiden määrä oli {esteidenMaara}");
        }
    }
    
    
    private void TormaysSuksi(PhysicsObject pingu, PhysicsObject suksi)
    {
        if (sydamet <= 3) sydamet += 1;
        sydanLaskuri.Value = sydamet;
        Console.WriteLine(sydamet);
        suksi.Destroy();
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
        
    }
}