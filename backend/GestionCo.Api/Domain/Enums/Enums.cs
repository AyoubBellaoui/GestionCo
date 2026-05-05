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
    AnnulationVente = 4
}

public enum TypeClient
{
    Entreprise = 1,
    Particulier = 2
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
