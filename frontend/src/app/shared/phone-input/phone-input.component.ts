import { Component, forwardRef } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';

interface Country { flag: string; name: string; dial: string; }

const COUNTRIES: Country[] = [
  // Afrique du Nord (priorité)
  { flag: '🇲🇦', name: 'Maroc',                    dial: '+212' },
  { flag: '🇩🇿', name: 'Algérie',                  dial: '+213' },
  { flag: '🇹🇳', name: 'Tunisie',                  dial: '+216' },
  { flag: '🇱🇾', name: 'Libye',                    dial: '+218' },
  { flag: '🇪🇬', name: 'Égypte',                   dial: '+20'  },
  { flag: '🇲🇷', name: 'Mauritanie',               dial: '+222' },
  { flag: '🇸🇩', name: 'Soudan',                   dial: '+249' },
  // Europe de l'Ouest
  { flag: '🇫🇷', name: 'France',                   dial: '+33'  },
  { flag: '🇧🇪', name: 'Belgique',                 dial: '+32'  },
  { flag: '🇨🇭', name: 'Suisse',                   dial: '+41'  },
  { flag: '🇪🇸', name: 'Espagne',                  dial: '+34'  },
  { flag: '🇵🇹', name: 'Portugal',                 dial: '+351' },
  { flag: '🇮🇹', name: 'Italie',                   dial: '+39'  },
  { flag: '🇩🇪', name: 'Allemagne',                dial: '+49'  },
  { flag: '🇬🇧', name: 'Royaume-Uni',              dial: '+44'  },
  { flag: '🇳🇱', name: 'Pays-Bas',                 dial: '+31'  },
  { flag: '🇦🇹', name: 'Autriche',                 dial: '+43'  },
  { flag: '🇱🇺', name: 'Luxembourg',               dial: '+352' },
  { flag: '🇮🇪', name: 'Irlande',                  dial: '+353' },
  { flag: '🇬🇷', name: 'Grèce',                    dial: '+30'  },
  { flag: '🇲🇨', name: 'Monaco',                   dial: '+377' },
  // Moyen-Orient
  { flag: '🇸🇦', name: 'Arabie Saoudite',          dial: '+966' },
  { flag: '🇦🇪', name: 'Émirats Arabes Unis',      dial: '+971' },
  { flag: '🇶🇦', name: 'Qatar',                    dial: '+974' },
  { flag: '🇰🇼', name: 'Koweït',                   dial: '+965' },
  { flag: '🇧🇭', name: 'Bahreïn',                  dial: '+973' },
  { flag: '🇴🇲', name: 'Oman',                     dial: '+968' },
  { flag: '🇯🇴', name: 'Jordanie',                 dial: '+962' },
  { flag: '🇱🇧', name: 'Liban',                    dial: '+961' },
  { flag: '🇮🇷', name: 'Iran',                     dial: '+98'  },
  { flag: '🇮🇶', name: 'Irak',                     dial: '+964' },
  { flag: '🇸🇾', name: 'Syrie',                    dial: '+963' },
  { flag: '🇾🇪', name: 'Yémen',                    dial: '+967' },
  { flag: '🇮🇱', name: 'Israël',                   dial: '+972' },
  { flag: '🇵🇸', name: 'Palestine',                dial: '+970' },
  // Reste de l'Afrique
  { flag: '🇸🇳', name: 'Sénégal',                  dial: '+221' },
  { flag: '🇨🇮', name: "Côte d'Ivoire",            dial: '+225' },
  { flag: '🇨🇲', name: 'Cameroun',                 dial: '+237' },
  { flag: '🇳🇬', name: 'Nigéria',                  dial: '+234' },
  { flag: '🇬🇭', name: 'Ghana',                    dial: '+233' },
  { flag: '🇰🇪', name: 'Kenya',                    dial: '+254' },
  { flag: '🇪🇹', name: 'Éthiopie',                 dial: '+251' },
  { flag: '🇿🇦', name: 'Afrique du Sud',           dial: '+27'  },
  { flag: '🇹🇿', name: 'Tanzanie',                 dial: '+255' },
  { flag: '🇺🇬', name: 'Ouganda',                  dial: '+256' },
  { flag: '🇷🇼', name: 'Rwanda',                   dial: '+250' },
  { flag: '🇧🇯', name: 'Bénin',                    dial: '+229' },
  { flag: '🇧🇫', name: 'Burkina Faso',             dial: '+226' },
  { flag: '🇧🇮', name: 'Burundi',                  dial: '+257' },
  { flag: '🇨🇻', name: 'Cabo Verde',               dial: '+238' },
  { flag: '🇨🇫', name: 'Centrafrique',             dial: '+236' },
  { flag: '🇰🇲', name: 'Comores',                  dial: '+269' },
  { flag: '🇨🇬', name: 'Congo (Rép.)',             dial: '+242' },
  { flag: '🇨🇩', name: 'Congo (RDC)',              dial: '+243' },
  { flag: '🇩🇯', name: 'Djibouti',                 dial: '+253' },
  { flag: '🇬🇶', name: 'Guinée équatoriale',       dial: '+240' },
  { flag: '🇪🇷', name: 'Érythrée',                 dial: '+291' },
  { flag: '🇬🇦', name: 'Gabon',                    dial: '+241' },
  { flag: '🇬🇲', name: 'Gambie',                   dial: '+220' },
  { flag: '🇬🇳', name: 'Guinée',                   dial: '+224' },
  { flag: '🇬🇼', name: 'Guinée-Bissau',            dial: '+245' },
  { flag: '🇱🇸', name: 'Lesotho',                  dial: '+266' },
  { flag: '🇱🇷', name: 'Libéria',                  dial: '+231' },
  { flag: '🇲🇬', name: 'Madagascar',               dial: '+261' },
  { flag: '🇲🇼', name: 'Malawi',                   dial: '+265' },
  { flag: '🇲🇱', name: 'Mali',                     dial: '+223' },
  { flag: '🇲🇺', name: 'Maurice',                  dial: '+230' },
  { flag: '🇲🇿', name: 'Mozambique',               dial: '+258' },
  { flag: '🇳🇦', name: 'Namibie',                  dial: '+264' },
  { flag: '🇳🇪', name: 'Niger',                    dial: '+227' },
  { flag: '🇸🇨', name: 'Seychelles',               dial: '+248' },
  { flag: '🇸🇱', name: 'Sierra Leone',             dial: '+232' },
  { flag: '🇸🇴', name: 'Somalie',                  dial: '+252' },
  { flag: '🇸🇸', name: 'Soudan du Sud',            dial: '+211' },
  { flag: '🇸🇿', name: 'Eswatini',                 dial: '+268' },
  { flag: '🇸🇹', name: 'São Tomé-et-Príncipe',     dial: '+239' },
  { flag: '🇹🇩', name: 'Tchad',                    dial: '+235' },
  { flag: '🇹🇬', name: 'Togo',                     dial: '+228' },
  { flag: '🇿🇲', name: 'Zambie',                   dial: '+260' },
  { flag: '🇿🇼', name: 'Zimbabwe',                 dial: '+263' },
  { flag: '🇦🇴', name: 'Angola',                   dial: '+244' },
  // Amériques
  { flag: '🇺🇸', name: 'États-Unis',               dial: '+1'   },
  { flag: '🇨🇦', name: 'Canada',                   dial: '+1'   },
  { flag: '🇲🇽', name: 'Mexique',                  dial: '+52'  },
  { flag: '🇧🇷', name: 'Brésil',                   dial: '+55'  },
  { flag: '🇦🇷', name: 'Argentine',                dial: '+54'  },
  { flag: '🇨🇱', name: 'Chili',                    dial: '+56'  },
  { flag: '🇨🇴', name: 'Colombie',                 dial: '+57'  },
  { flag: '🇵🇪', name: 'Pérou',                    dial: '+51'  },
  { flag: '🇻🇪', name: 'Venezuela',                dial: '+58'  },
  { flag: '🇪🇨', name: 'Équateur',                 dial: '+593' },
  { flag: '🇧🇴', name: 'Bolivie',                  dial: '+591' },
  { flag: '🇵🇾', name: 'Paraguay',                 dial: '+595' },
  { flag: '🇺🇾', name: 'Uruguay',                  dial: '+598' },
  { flag: '🇬🇾', name: 'Guyana',                   dial: '+592' },
  { flag: '🇸🇷', name: 'Suriname',                 dial: '+597' },
  { flag: '🇨🇺', name: 'Cuba',                     dial: '+53'  },
  { flag: '🇩🇴', name: 'Rép. dominicaine',         dial: '+1809'},
  { flag: '🇭🇹', name: 'Haïti',                    dial: '+509' },
  { flag: '🇯🇲', name: 'Jamaïque',                 dial: '+1876'},
  { flag: '🇵🇦', name: 'Panama',                   dial: '+507' },
  { flag: '🇨🇷', name: 'Costa Rica',               dial: '+506' },
  { flag: '🇬🇹', name: 'Guatemala',                dial: '+502' },
  { flag: '🇭🇳', name: 'Honduras',                 dial: '+504' },
  { flag: '🇸🇻', name: 'El Salvador',              dial: '+503' },
  { flag: '🇳🇮', name: 'Nicaragua',                dial: '+505' },
  { flag: '🇹🇹', name: 'Trinité-et-Tobago',        dial: '+1868'},
  { flag: '🇧🇧', name: 'Barbade',                  dial: '+1246'},
  { flag: '🇧🇸', name: 'Bahamas',                  dial: '+1242'},
  { flag: '🇧🇿', name: 'Belize',                   dial: '+501' },
  // Asie
  { flag: '🇨🇳', name: 'Chine',                    dial: '+86'  },
  { flag: '🇯🇵', name: 'Japon',                    dial: '+81'  },
  { flag: '🇰🇷', name: 'Corée du Sud',             dial: '+82'  },
  { flag: '🇰🇵', name: 'Corée du Nord',            dial: '+850' },
  { flag: '🇮🇳', name: 'Inde',                     dial: '+91'  },
  { flag: '🇵🇰', name: 'Pakistan',                 dial: '+92'  },
  { flag: '🇧🇩', name: 'Bangladesh',               dial: '+880' },
  { flag: '🇱🇰', name: 'Sri Lanka',                dial: '+94'  },
  { flag: '🇳🇵', name: 'Népal',                    dial: '+977' },
  { flag: '🇲🇲', name: 'Myanmar',                  dial: '+95'  },
  { flag: '🇹🇭', name: 'Thaïlande',               dial: '+66'  },
  { flag: '🇻🇳', name: 'Viêt Nam',                 dial: '+84'  },
  { flag: '🇰🇭', name: 'Cambodge',                 dial: '+855' },
  { flag: '🇱🇦', name: 'Laos',                     dial: '+856' },
  { flag: '🇵🇭', name: 'Philippines',              dial: '+63'  },
  { flag: '🇮🇩', name: 'Indonésie',               dial: '+62'  },
  { flag: '🇲🇾', name: 'Malaisie',                 dial: '+60'  },
  { flag: '🇸🇬', name: 'Singapour',                dial: '+65'  },
  { flag: '🇧🇳', name: 'Brunéi',                   dial: '+673' },
  { flag: '🇹🇼', name: 'Taïwan',                   dial: '+886' },
  { flag: '🇭🇰', name: 'Hong Kong',                dial: '+852' },
  { flag: '🇲🇴', name: 'Macao',                    dial: '+853' },
  { flag: '🇲🇳', name: 'Mongolie',                 dial: '+976' },
  { flag: '🇰🇿', name: 'Kazakhstan',               dial: '+7'   },
  { flag: '🇺🇿', name: 'Ouzbékistan',              dial: '+998' },
  { flag: '🇹🇯', name: 'Tadjikistan',              dial: '+992' },
  { flag: '🇹🇲', name: 'Turkménistan',             dial: '+993' },
  { flag: '🇰🇬', name: 'Kirghizistan',             dial: '+996' },
  { flag: '🇦🇫', name: 'Afghanistan',              dial: '+93'  },
  { flag: '🇧🇹', name: 'Bhoutan',                  dial: '+975' },
  { flag: '🇲🇻', name: 'Maldives',                 dial: '+960' },
  { flag: '🇹🇱', name: 'Timor-Leste',              dial: '+670' },
  // Europe de l'Est & CEI
  { flag: '🇷🇺', name: 'Russie',                   dial: '+7'   },
  { flag: '🇺🇦', name: 'Ukraine',                  dial: '+380' },
  { flag: '🇵🇱', name: 'Pologne',                  dial: '+48'  },
  { flag: '🇷🇴', name: 'Roumanie',                 dial: '+40'  },
  { flag: '🇨🇿', name: 'Tchéquie',                 dial: '+420' },
  { flag: '🇸🇰', name: 'Slovaquie',                dial: '+421' },
  { flag: '🇭🇺', name: 'Hongrie',                  dial: '+36'  },
  { flag: '🇧🇬', name: 'Bulgarie',                 dial: '+359' },
  { flag: '🇷🇸', name: 'Serbie',                   dial: '+381' },
  { flag: '🇭🇷', name: 'Croatie',                  dial: '+385' },
  { flag: '🇸🇮', name: 'Slovénie',                 dial: '+386' },
  { flag: '🇧🇦', name: 'Bosnie-Herzégovine',       dial: '+387' },
  { flag: '🇲🇰', name: 'Macédoine du Nord',        dial: '+389' },
  { flag: '🇦🇱', name: 'Albanie',                  dial: '+355' },
  { flag: '🇲🇪', name: 'Monténégro',               dial: '+382' },
  { flag: '🇽🇰', name: 'Kosovo',                   dial: '+383' },
  { flag: '🇧🇾', name: 'Bélarus',                  dial: '+375' },
  { flag: '🇲🇩', name: 'Moldova',                  dial: '+373' },
  { flag: '🇬🇪', name: 'Géorgie',                  dial: '+995' },
  { flag: '🇦🇲', name: 'Arménie',                  dial: '+374' },
  { flag: '🇦🇿', name: 'Azerbaïdjan',              dial: '+994' },
  { flag: '🇱🇻', name: 'Lettonie',                 dial: '+371' },
  { flag: '🇱🇹', name: 'Lituanie',                 dial: '+370' },
  { flag: '🇪🇪', name: 'Estonie',                  dial: '+372' },
  { flag: '🇫🇮', name: 'Finlande',                 dial: '+358' },
  { flag: '🇸🇪', name: 'Suède',                    dial: '+46'  },
  { flag: '🇳🇴', name: 'Norvège',                  dial: '+47'  },
  { flag: '🇩🇰', name: 'Danemark',                 dial: '+45'  },
  { flag: '🇮🇸', name: 'Islande',                  dial: '+354' },
  { flag: '🇨🇾', name: 'Chypre',                   dial: '+357' },
  { flag: '🇲🇹', name: 'Malte',                    dial: '+356' },
  { flag: '🇱🇮', name: 'Liechtenstein',            dial: '+423' },
  { flag: '🇸🇲', name: 'Saint-Marin',              dial: '+378' },
  { flag: '🇦🇩', name: 'Andorre',                  dial: '+376' },
  { flag: '🇻🇦', name: 'Vatican',                  dial: '+39'  },
  { flag: '🇹🇷', name: 'Turquie',                  dial: '+90'  },
  // Océanie
  { flag: '🇦🇺', name: 'Australie',                dial: '+61'  },
  { flag: '🇳🇿', name: 'Nouvelle-Zélande',         dial: '+64'  },
  { flag: '🇫🇯', name: 'Fidji',                    dial: '+679' },
  { flag: '🇵🇬', name: 'Papouasie-Nvl-Guinée',     dial: '+675' },
  { flag: '🇸🇧', name: 'Îles Salomon',             dial: '+677' },
  { flag: '🇻🇺', name: 'Vanuatu',                  dial: '+678' },
  { flag: '🇼🇸', name: 'Samoa',                    dial: '+685' },
  { flag: '🇹🇴', name: 'Tonga',                    dial: '+676' },
  { flag: '🇰🇮', name: 'Kiribati',                 dial: '+686' },
  { flag: '🇫🇲', name: 'Micronésie',               dial: '+691' },
  { flag: '🇲🇭', name: 'Îles Marshall',            dial: '+692' },
  { flag: '🇵🇼', name: 'Palaos',                   dial: '+680' },
  { flag: '🇳🇷', name: 'Nauru',                    dial: '+674' },
  { flag: '🇹🇻', name: 'Tuvalu',                   dial: '+688' },
];

@Component({
  selector: 'app-phone-input',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="phone-input-wrap">
      <select class="phone-country-select" [(ngModel)]="selectedDial" (ngModelChange)="emit()">
        @for (c of countries; track c.dial + c.name) {
          <option [value]="c.dial">{{ c.flag }} {{ c.dial }}</option>
        }
      </select>
      <input type="tel" class="form-input phone-number-input"
             [value]="localNumber"
             (input)="onInput($any($event.target).value)"
             (blur)="onTouched()" />
    </div>
  `,
  styles: [`
    .phone-input-wrap {
      display: flex;
      gap: 0;
      align-items: stretch;
    }
    .phone-country-select {
      flex-shrink: 0;
      height: 40px;
      padding: 0 8px;
      border: 1px solid var(--border);
      border-right: none;
      border-radius: var(--radius, 8px) 0 0 var(--radius, 8px);
      background: var(--surface2);
      color: var(--text);
      font-size: 13px;
      cursor: pointer;
      outline: none;
      min-width: 90px;
      appearance: none;
      -webkit-appearance: none;
      background-image: url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='10' height='10' viewBox='0 0 24 24' fill='none' stroke='%236b7280' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><polyline points='6 9 12 15 18 9'></polyline></svg>");
      background-repeat: no-repeat;
      background-position: right 6px center;
      padding-right: 22px;
    }
    .phone-country-select:focus {
      border-color: var(--primary);
      z-index: 1;
    }
    .phone-number-input {
      border-radius: 0 var(--radius, 8px) var(--radius, 8px) 0 !important;
      flex: 1;
    }
  `],
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => PhoneInputComponent),
    multi: true
  }]
})
export class PhoneInputComponent implements ControlValueAccessor {
  readonly countries = COUNTRIES;
  selectedDial = '+212';
  localNumber = '';

  private _onChange: (v: string) => void = () => {};
  onTouched: () => void = () => {};

  writeValue(value: string): void {
    if (!value) { this.selectedDial = '+212'; this.localNumber = ''; return; }
    // Sort by dial length descending so longer codes match first (e.g. +1868 before +1)
    const sorted = [...COUNTRIES].sort((a, b) => b.dial.length - a.dial.length);
    const match = sorted.find(c => value.startsWith(c.dial));
    if (match) {
      this.selectedDial = match.dial;
      this.localNumber = value.slice(match.dial.length).trimStart();
    } else {
      this.selectedDial = '+212';
      this.localNumber = value;
    }
  }

  registerOnChange(fn: (v: string) => void): void { this._onChange = fn; }
  registerOnTouched(fn: () => void): void { this.onTouched = fn; }

  onInput(val: string): void { this.localNumber = val; this.emit(); }

  emit(): void {
    const full = this.localNumber.trim()
      ? `${this.selectedDial} ${this.localNumber.trim()}`
      : '';
    this._onChange(full);
  }
}
