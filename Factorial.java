public class Factorial {
    public static int printFact(int n) {
        if (n == 1) {
            return 1;
        }
        int k = printFact(n - 1);
        return n * k;
    }

    public static void main(String[] args) {
        System.out.println(printFact(5));
    }
}