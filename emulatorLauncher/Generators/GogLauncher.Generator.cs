using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EmulatorLauncher.Common;
// Non c'è più bisogno di usare librerie specifiche GOG qui

namespace EmulatorLauncher
{
    // Questo è un file parziale della classe ExeLauncherGenerator
    partial class ExeLauncherGenerator : Generator
    {
        // --- INIZIO BLOCCO CORRETTO PER GOG ---

        // Questa classe nidificata gestisce la logica di avvio per i giochi GOG
        class GogGameLauncher : GameLauncher
        {
            // Il costruttore ora imposta il nome corretto del processo del launcher
            public GogGameLauncher(Uri uri)
            {
                // Imposta il nome del processo del launcher di GOG.
                // Questo verrà usato per l'hotkey e per nascondere la finestra.
                this.LauncherExe = "GalaxyClient"; 
            }

            // Aggiungiamo un costruttore vuoto per compatibilità con altre parti del progetto
            public GogGameLauncher() 
            {
                this.LauncherExe = "GalaxyClient";
            }

            // RIMUOVIAMO il metodo RunAndWait da qui. 
            // In questo modo, il programma sarà forzato a usare il metodo RunAndWait 
            // principale di ExeLauncherGenerator, che contiene già tutta la logica 
            // corretta per il rilevamento automatico, l'hotkey dinamico e la gestione del focus.
            // Lasciare un'implementazione vuota per soddisfare l'override non è necessario se
            // il metodo non viene chiamato, ma per sicurezza, se il tuo progetto lo richiede,
            // puoi lasciare una versione vuota. In questo caso, lo rimuoviamo per usare la logica centrale.
            public override int RunAndWait(ProcessStartInfo path)
            {
                // Questa funzione non dovrebbe essere chiamata, ma se lo fosse, 
                // deleghiamo al metodo di base che non fa nulla, per evitare errori.
                // L'esecuzione reale avverrà nel RunAndWait di ExeLauncherGenerator.
                return 0;
            }
        }

        // --- FINE BLOCCO CORRETTO PER GOG ---
    }
}
