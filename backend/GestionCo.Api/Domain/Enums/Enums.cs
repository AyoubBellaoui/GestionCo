namespace GestionCo.Api.Domain.Enums;

public enum RoleUtilisateur
{
    Admin = 1,
    Gestionnaire = 2,
    Client = 3
}

public enum StatutVente
{
    EnAttente = 1,
    Paye = 2,
    Annule = 3
}

public enum StatutFacture
{
    EnAttente = 1,
    Payee = 2,
    EnRetard = 3,
    Annulee = 4,
    PartiellementPayee = 5
}

public enum MethodePaiement
{
    Espece = 1,
    Carte = 2,
    Virement = 3,
    Cheque = 4
}

public enum StatutAchat
{
    EnAttente = 1,
    Partiel = 2,
    Paye = 3,
    Annule = 4
}

public enum StatutPaiement
{
    Confirme = 1,
    EnAttente = 2,
    Annule = 3
}

public enum TypeMouvementStock
{
    Entree = 1,
    Sortie = 2
}

public enum SourceMouvementStock
{
    Achat = 1,
    Vente = 2,
    Manuel = 3,
    AnnulationVente = 4,
    ModificationVente = 5,
    AnnulationAchat = 6,
    ModificationAchat = 7
}

public enum TypeClient
{
    Entreprise = 1,
    Particulier = 2
}

public enum TypeNotification
{
    Info = 1,
    Success = 2,
    Warning = 3,
    Danger = 4
}

public enum CategorieNotification
{
    Vente = 1,
    Achat = 2,
    Charge = 3,
    Paiement = 4,
    Stock = 5,
    Facture = 6,
    Systeme = 7
}

public enum StatutCharge
{
    EnAttente = 1,
    Partiel = 2,
    Paye = 3,
    Annule = 4
}

public enum StatutDevis
{
    Brouillon = 1,
    Envoye = 2,
    Accepte = 3,
    Refuse = 4,
    Expire = 5,
    Converti = 6
}

public enum StatutCommande
{
    EnAttente  = 1,
    Confirmee  = 2,
    EnCours    = 3,
    Livree     = 4,
    Convertie  = 5,
    Annulee    = 6
}

public enum ActionLog
{
    Create = 1,
    Update = 2,
    Delete = 3,
    Login = 4,
    Logout = 5,
    Sensitive = 6
}
