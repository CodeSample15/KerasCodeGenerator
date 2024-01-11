using UnityEngine.Assertions;

public class Shape
{
    public int[] sizes;

    public Shape(int size) {
        sizes = new int[size];
    }

    public Shape(int[] sizes) {
        this.sizes = sizes;
    }

    public bool isEqualTo(Shape other) { //could i just use the .compare of the two .toStrings? yes. will I? No, this is fancier
        if(other.sizes.Length != sizes.Length)
            return false;

        for(int i=0; i<sizes.Length; i++) {
            if(other.sizes[i] != sizes[i])
                return false;
        }

        return true;
    }

    public string toString() 
    {
        string output = "";
        for(int i=0; i<sizes.Length; i++) {
            output += sizes[i];
            if(i!=sizes.Length-1)
                output += 'x';
        }

        return output;
    }
}
