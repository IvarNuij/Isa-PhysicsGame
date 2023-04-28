using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilePhysics : BaseClass
{
    public int GeneralPhysicsSpeed;
    private int generalPhysicsTimer;
    public int TempDifferenceMagnitude; //The higher the less temp difference makes a difference

    private Dictionary<ID, PhysicsTile> PhysicTiles = new Dictionary<ID, PhysicsTile>();
    private HashSet<Vector2Int> skipTiles = new HashSet<Vector2Int>();

    private Tile currentTile;
    private PhysicsTile currentPhysics;

    private MainGrid mainGrid;

    public override void OnAwake()
    {
        mainGrid = FindObjectOfType<MainGrid>();
    }

    public override void OnStart()
    {
        //Tile Physics properties (probably shouldn't hardcode)

        //                  ID                       ID                    solid  liquid  gas  UPS  Grav  Max SP Th  -----MinTemp-------     -------MaxTemp--------
        PhysicTiles.Add(ID.dirt, new PhysicsTile(ID.dirt,                   true, false, false,  1, true,  9, 5,  5, true,  25000, ID.grass, true,  5000000, ID.carbonDioxite));
        PhysicTiles.Add(ID.grass, new PhysicsTile(ID.grass,                 true, false, false,  1, true,  9, 5,  5, false,     0, ID.none,  true,    50000, ID.dirt));
        PhysicTiles.Add(ID.water, new PhysicsTile(ID.water,                 false, true, false,  1, true,  9, 9, 10, true,  -1000, ID.ice,   true,   100000, ID.steam));
        PhysicTiles.Add(ID.stone, new PhysicsTile(ID.stone,                 true, false, false, 20, false, 9, 9,  1, false,     0, ID.none,  true, 10000000, ID.carbonDioxite));
        PhysicTiles.Add(ID.ice, new PhysicsTile(ID.ice,                     true, false, false, 20, false, 9, 1, 10, false,     0, ID.none,  true,     1000, ID.water));
        PhysicTiles.Add(ID.carbonDioxite, new PhysicsTile(ID.carbonDioxite, false, false, true, 20, false, 9, 1, 15, false,     0, ID.none,  false,       0, ID.none));
        PhysicTiles.Add(ID.oxygen, new PhysicsTile(ID.oxygen,               false, false, true, 20, false, 9, 1,  6, false,     0, ID.none,  false,       0, ID.none));
        PhysicTiles.Add(ID.steam, new PhysicsTile(ID.steam,                 false, false, true, 20, false, 9, 1, 10, true,  96000, ID.water, false,       0, ID.none));
    }

    public override void OnUPS()
    {
        Physics();
    }

    private void Physics()
    {
        //Check entire grid (probably inefficient)
        for (int y = mainGrid.Height - 1; y >= 1 ; y--)
        {
            for (int x = 1; x < mainGrid.Width - 1; x++)
            {
                currentTile = mainGrid.GetTile(new Vector2Int(x, y));

                if (!skipTiles.Contains(currentTile.pos) && PhysicTiles.ContainsKey(currentTile.id))
                {
                    PhysicTiles.TryGetValue(currentTile.id, out currentPhysics);

                    //General physics-----------------------------------------------------
                    if (generalPhysicsTimer >= GeneralPhysicsSpeed)
                    {
                        GeneralPhysics(x, y);
                    }
                    else
                    {
                        generalPhysicsTimer++;
                    }

                    //Seperate Tile Physics------------------------------------------------
                    if (currentTile.currentUpdate >= currentPhysics.updateSpeed)
                    {
                        SeperateTilePhysics(x, y);
                    }
                    else
                    {
                        currentTile.currentUpdate++;
                    }
                }
            }
        }
        skipTiles.Clear();
    }

    //-----------------------------------------------------------------------

    private void GeneralPhysics(int x, int y)
    {
        generalPhysicsTimer = 0;

        //Temperature
        Tile upTile = mainGrid.GetTile(new Vector2Int(x, y - 1));
        Tile downTile = mainGrid.GetTile(new Vector2Int(x, y + 1));
        Tile rightTile = mainGrid.GetTile(new Vector2Int(x + 1, y));
        Tile leftTile = mainGrid.GetTile(new Vector2Int(x - 1, y));

        TempPhysics(upTile, downTile, rightTile, leftTile);
    }

    private void SeperateTilePhysics(int x, int y)
    {
        currentTile.currentUpdate = 0;

        //Gravity
        Tile downTile = mainGrid.GetTile(new Vector2Int(x, y + 1));
        if (currentPhysics.hasGravity)
        {
            GravityPhysics(downTile);
        }

        //Solid

        //Liquid
        if (currentPhysics.isLiquid)
        {
            LiquidPhysics(x, y);
        }

        //Gas
        if (currentPhysics.isGas)
        {
            GasPhysics(x, y);
        }
    }

    //-------------------------------------------------------------------------

    private void PlantPhysics()
    {

    }

    private void GasPhysics(int x, int y)
    {
        int randomValue = Random.Range(0, 99);
        if (randomValue <= 24) //up
        {
            Tile upTile = mainGrid.GetTile(new Vector2Int(x, y - 1));
            if (upTile.amount < currentTile.amount)
            {
                mainGrid.MoveTile(currentTile, upTile, currentPhysics.maxAmount, currentPhysics.speed);
            }
        }
        else if (randomValue >= 25 && randomValue <= 49) //right
        {
            Tile rightTile = mainGrid.GetTile(new Vector2Int(x + 1, y));
            if (rightTile.amount < currentTile.amount)
            {
                mainGrid.MoveTile(currentTile, rightTile, currentPhysics.maxAmount, currentPhysics.speed);
            }
        }
        else if (randomValue >= 50 && randomValue <= 74) //down
        {
            Tile downTile = mainGrid.GetTile(new Vector2Int(x, y + 1));
            if (downTile.amount < currentTile.amount)
            {
                mainGrid.MoveTile(currentTile, downTile, currentPhysics.maxAmount, currentPhysics.speed);
            }
        }
        else //left
        {
            Tile leftTile = mainGrid.GetTile(new Vector2Int(x - 1, y));
            if (leftTile.amount < currentTile.amount)
            {
                mainGrid.MoveTile(currentTile, leftTile, currentPhysics.maxAmount, currentPhysics.speed);
            }
        }
    }

    private void LiquidPhysics(int x, int y)
    {
        if (Random.Range(0, 99) > 50)
        {
            Tile leftTile = mainGrid.GetTile(new Vector2Int(x - 1, y));
            LiquidMoveTile(leftTile);
            Tile rightTile = mainGrid.GetTile(new Vector2Int(x + 1, y));
            LiquidMoveTile(rightTile);
        }
        else
        {
            Tile rightTile = mainGrid.GetTile(new Vector2Int(x + 1, y));
            LiquidMoveTile(rightTile);
            Tile leftTile = mainGrid.GetTile(new Vector2Int(x - 1, y));
            LiquidMoveTile(leftTile);
        }
    }

    private void LiquidMoveTile(Tile targetTile)
    {

        if (targetTile.id == ID.none || targetTile.id == currentTile.id)
        {
            if (targetTile.amount < currentTile.amount)
            {
                mainGrid.MoveTile(currentTile, targetTile, currentPhysics.maxAmount, currentPhysics.speed);
            }
        }
    }

    private void TempPhysics(Tile upTile, Tile downTile, Tile rightTile, Tile leftTile)
    {
        //UpTile
        if (upTile.id != ID.none)
        {
            PhysicTiles.TryGetValue(upTile.id, out PhysicsTile upTilePhysics);
            if (upTilePhysics != null)
            {
                CalcTemp(upTile, upTilePhysics);
            }
        }

        //DownTile
        if (downTile.id != ID.none)
        {
            PhysicTiles.TryGetValue(downTile.id, out PhysicsTile downTilePhysics);
            if (downTilePhysics != null)
            {
                CalcTemp(downTile, downTilePhysics);
            }
        }

        //RightTile
        if (rightTile.id != ID.none)
        {
            PhysicTiles.TryGetValue(rightTile.id, out PhysicsTile rightTilePhysics);
            if (rightTilePhysics != null)
            {
                CalcTemp(rightTile, rightTilePhysics);
            }
        }

        //LeftTile
        if (leftTile.id != ID.none)
        {
            PhysicTiles.TryGetValue(leftTile.id, out PhysicsTile leftTilePhysics);
            if (leftTilePhysics != null)
            {
                CalcTemp(leftTile, leftTilePhysics);
            }
        }

        //MinTemp
        if (currentPhysics.hasMinTemp)
        {
            if (currentTile.temp <= currentPhysics.minTemp)
            {
                mainGrid.SetTile(currentTile.pos, currentPhysics.ifMinTemp, currentTile.amount, currentTile.temp);
            }
        }

        //MaxTemp
        if (currentPhysics.hasMaxTemp)
        {
            if (currentTile.temp >= currentPhysics.maxTemp)
            {
                mainGrid.SetTile(currentTile.pos, currentPhysics.ifMaxTemp, currentTile.amount, currentTile.temp);
            }
        }
    }

    private void CalcTemp(Tile targetTile, PhysicsTile targetPhysics)
    {
        int tempDifference = Mathf.Abs(currentTile.temp - targetTile.temp) / TempDifferenceMagnitude;
        if (currentTile.temp > targetTile.temp)
        {
            if (currentPhysics.thermalConductivity > targetPhysics.thermalConductivity)
            {
                currentTile.temp -= targetPhysics.thermalConductivity + tempDifference;
                targetTile.temp += targetPhysics.thermalConductivity + tempDifference;
            }
            else
            {
                currentTile.temp -= currentPhysics.thermalConductivity + tempDifference;
                targetTile.temp += currentPhysics.thermalConductivity + tempDifference;
            }
        }
        else
        {
            if (currentPhysics.thermalConductivity > targetPhysics.thermalConductivity)
            {
                currentTile.temp += targetPhysics.thermalConductivity + tempDifference;
                targetTile.temp -= targetPhysics.thermalConductivity + tempDifference;
            }
            else
            {
                currentTile.temp += currentPhysics.thermalConductivity + tempDifference;
                targetTile.temp -= currentPhysics.thermalConductivity + tempDifference;
            }
        }
    }

    private void GravityPhysics(Tile downTile)
    {
        PhysicTiles.TryGetValue(downTile.id, out PhysicsTile downTilePhysics);

        if (downTilePhysics != null) 
        {
            if (downTilePhysics.isLiquid)
            {
                mainGrid.SwitchTiles(currentTile, downTile);
            }
            else if (downTilePhysics.isGas)
            {
                mainGrid.SwitchTiles(currentTile, downTile);
            }
        }
        else
        {
            mainGrid.MoveTile(currentTile, downTile, currentPhysics.maxAmount, currentPhysics.speed);
        }
    }
}