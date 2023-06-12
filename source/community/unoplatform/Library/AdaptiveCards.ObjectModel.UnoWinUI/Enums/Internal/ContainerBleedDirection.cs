namespace AdaptiveCards.ObjectModel.WinUI3;

// We have to define all possible combinations because java doesn't allow bitwise operations between enum values
// and it also limits the values an enum can have to only the values defined in the enum, so combinations wouldn't be
// allowed unless they have been explicitly declared (i.e. 0x0101 wouldn't be valid as it was not part of the declared values)
internal enum ContainerBleedDirection
{
    BleedRestricted = 0x0000,
    BleedLeft = 0x0001,
    BleedRight = 0x0010,
    BleedLeftRight = 0x0011,
    BleedUp = 0x0100,
    BleedLeftUp = 0x0101,
    BleedRightUp = 0x0110,
    BleedLeftRightUp = 0x0111,
    BleedDown = 0x1000,
    BleedLeftDown = 0x1001,
    BleedRightDown = 0x1010,
    BleedLeftRightDown = 0x1011,
    BleedUpDown = 0x1100,
    BleedLeftUpDown = 0x1101,
    BleedRightUpDown = 0x1110,
    BleedAll = 0x1111
}
