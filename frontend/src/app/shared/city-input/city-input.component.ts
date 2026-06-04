import { Component, forwardRef, HostListener, ElementRef } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';

const MOROCCO_CITIES: string[] = [
  // ─── Région Grand Casablanca-Settat ───
  'Casablanca','Mohammedia','Settat','Berrechid','El Jadida','Sidi Bennour',
  'Ben Ahmed','Benslimane','Médiouna','Nouaceur','Bouskoura','Dar Bouazza',
  'Lahraouiyine','Aïn Harrouda','Aïn Chock','Ain Sebaa','Hay Hassani',
  'Azemmour','Beni Meskine','Bni Yakhlef','Bouznika','Cherrat','El Mansouria',
  'Oulad Fares','Oulad Haj','Sidi Rahhal','Sidi Smail','Tit Mellil',
  'Zaouia des Ameur','Bni Khloug','Dar Chafai','El Gara','Had Soualem',
  'Kerouna','Lemarigat','Oulad Berhil','Oulad Said','Oulad Youssef',
  'Sidi Aïssa','Sidi Mbarek','Sidi Said','Sidi Yahia Zaer','Lahraouiyine',
  // ─── Région Rabat-Salé-Kénitra ───
  'Rabat','Salé','Kénitra','Skhirate','Témara','Khémisset','Sidi Slimane',
  'Sidi Kacem','Souk El Arbaa','Tiflet','Sidi Taibi','Aïn Aouda','Harhoura',
  'Bouknadel','Mehdia','Lalla Mimouna','Mechraa Bel Ksiri','Moulay Bousselham',
  'Rommani','Sidi Allal El Bahraoui','Sidi Allal Tazi','Sidi Bettache',
  'Sidi Yahia Du Gharb','Teroual','Ain Johra','Ain Maatouf','Akreuch',
  'Beni Malek','Bradia','Ezzhiliga','Had Rharbia','Khnichet','Ksiba Mzouda',
  'Moulay Driss Zerhoun','Ouled Adou','Sebt Dafali','Sidi Abdallah',
  'Sidi Lamine','Tiddas','Ouled Slama','Arbaoua','Ain Defali','Sidi Bou Othman',
  // ─── Région Tanger-Tétouan-Al Hoceïma ───
  'Tanger','Tétouan','Al Hoceïma','Larache','Ksar El Kébir','Chefchaouen',
  'Martil','M\'diq','Fnideq','Asilah','Ouezzane','Targuist','Imzouren',
  'Bni Bouayach','Jebha','Oued Laou','Bab Berred','Aïn Aicha','Aïn Défali',
  'Bni Karrich','Bni Gmil','Bni Idder','Ghafsai','Issaguen','Ketama',
  'Rhafsai','Stehat','Tlata Rissana','Tizgane','Zoumi','El Jebha','Kaa Asras',
  'Oued Laou','Ras El Ma','Senhaja Sraïr','Tirhnimine','Bab Taza',
  'Dardara','Tamorot','Tizgane','Bni Selman','Ain Mediouna',
  // ─── Région L\'Oriental ───
  'Oujda','Nador','Berkane','Taourirt','Jerada','Bouarfa','Figuig','Driouch',
  'Saïdia','Ahfir','Aïn Bni Mathar','Touissit','Zaïo','Segangane',
  'Beni Drar','Laaroui','Beni Enzar','Midar','Aklim','El Aïoun Sidi Mellouk',
  'Hammam Boughrara','Guercif','Madagh','Mechraa Hammadi','Sidi Bouhria',
  'Tiztoutine','Tafoghalt','Talsint','Tendrara','Ain Kerma','Ain Sfa',
  'Beni Touzine','Laaouinate','Zaiou','Ain Beni Mathar','Monte Arrouit',
  'Ras El Aïoun','Selouane','Sidi Slimane Moul El Kifane','Sidi Yahia',
  'Taourirt','Taza','Ain Sfa','Bni Oukil','Djerada','El Aïoun',
  // ─── Région Fès-Meknès ───
  'Fès','Meknès','Taza','Ifrane','Azrou','El Hajeb','Sefrou',
  'Moulay Idriss Zerhoun','Khenifra','Midelt','Imouzzer Kandar','Ain Leuh',
  'Ain Taoujdate','Ain Jemaa','Almis Marmoucha','Bhalil','Boulmane',
  'El Menzel','El Mers','Ghafsai','Guigou','Imouzzer Marmoucha','Itzer',
  'Kandar','M\'Rirt','Moulay Yacoub','Ouaoumana','Ribat El Kheir',
  'Sebt Bni Garfett','Sidi Addi','Tafajight','Taineste','Tazouta',
  'Timahdite','Tounfite','Zellidja','Zrigat','Aghbalou','Ahermoumou',
  'Ait Ishaq','Anefgou','Boured','Dhar El Souk','El Borj','El Mhaya',
  'Guigou','Hlila','Jbabra','Kerrouchen','Khénifra','Lqliaa','Oued Amlil',
  'Oulmes','Ribat','Sebt Jahjouh','Skoura Mdaz','Taghzout','Tataouite',
  'Tizi N\'Trert','Aïn Tizgha','Aouinat Torkoz','Arazan','Ayt Bazza',
  'Bni Amart','Bni Ftah','Bni Rzine','Btarna','Dar El Hamra','El Aïoun',
  'El Orjane','Galaz','Gueldaman','Kelaa Des Mgouna','Khlalfa',
  'Mhaya','Mkansa','Msemrir','Naour','Oulad Tayeb','Ras Tabouda',
  'Saka','Sidi Bousserhane','Sidi El Makhfi','Sidi Yahia El Gharb',
  'Tahla','Taounate','Tissa','Zerarda',
  // ─── Région Béni Mellal-Khénifra ───
  'Béni Mellal','Khouribga','Fquih Ben Salah','Kasba Tadla','Azilal',
  'Demnate','Oued Zem','Boujad','Souk Sebt','El Ksiba','Afourer',
  'Zawyat Cheikh','Oulad Ayad','Bin El Ouidane','Aït Attab',
  'Aït Bou Oulli','Aït Majden','Aït Oumdeis','Bzou','Foum El Anser',
  'Hattane','Kerrouchen','Ksiba','Maâziz','Ouaouizerth','Oulad Mbark',
  'Oulad Saïd','Sidi Bou Ali','Sidi Hassane','Skhour Rehamna',
  'Souk El Had','Tadla','Tanourdi','Znada','Aït Imi','Aït Rbia',
  'Ait Tamlil','Aouguerssif','Bradia','Dar Ould Zidouh','Igoulmimane',
  'Lahbitat','Ouled Achraa','Ouled Yaïch','Sidi Jaber','Zaouiet Cheikh',
  // ─── Région Marrakech-Safi ───
  'Marrakech','Safi','Essaouira','El Kelaa des Sraghna','Chichaoua',
  'Youssoufia','Ben Guerir','Imintanoute','Tahanaout','Amizmiz',
  'Aït Ourir','Aït Faska','Aït Daoud','Asni','Imlil','Smimou',
  'Lalla Takerkoust','Ghar Laou','Kelaa des Sraghna','Rehamna',
  'Sidi Bou Othmane','Sidi Rahhal','Aït Benhaddou','Chemaïa',
  'El Attawia','Had Draâ','Had Hrara','Had Lgharbia','Ijoukak',
  'Imintanoute','Jemaa Shaim','Kerroum','Khmis Znata','Lqliaa',
  'Loudaya','Moulay Brahim','Nfis','Niaat','Oum Laachar','Ourika',
  'Roudane','Sidi Abdallah Ghiat','Sidi Bouzid','Sidi Ghanem',
  'Sidi Zouine','Souk Larbaa Sraghna','Souira Kdima','Tamallalt',
  'Tamazouzte','Tameslouht','Tamraght','Tanant','Tighdouine',
  'Tiksit','Touama','Toumlilte','Aït Imeghrane','Achour','Agadir Melloul',
  'Aghbalou N\'Kerdous','Agni','Aguerd','Aït Bouzid','Ain Tazitounte',
  'Akrich','Arbaa Sahel','Aymane','Bouabout','Deghnin','Echemmaia',
  'El Faid','Ghmate','Had Rhamna','Had Oulad Fraj','Khenifis','Lamrasla',
  'Lahlalfa','Lalla Aziza','M\'Semrir','Moqrisset','Ntifa','Sbaa Aiyoune',
  'Sidi Hssain','Tamzouzte','Taouloukoult','Tazenakht','Tilling',
  'Tizgui','Oulad Nemra','Rhamna','Sebt Gzoula','Sebt Jahjouh',
  'Sebt Limnabha','Sid Bou Othmane','Sidi Ali Ben Hamdouche',
  // ─── Région Souss-Massa ───
  'Agadir','Tiznit','Taroudant','Inezgane','Aït Melloul','Biougra',
  'Chtouka Aït Baha','Dcheira El Jihadia','Aourir','Oulad Teima',
  'Aït Baha','Tafraout','Massa','Sidi Ifni','Aglou','Mirleft',
  'Sidi Moussa Aglou','Aoulouz','Arazane','Argana','Assaka',
  'El Guerdane','Ida Ou Gnidif','Ida Ou Moumen','Imouzzer Ida Outanane',
  'Kasbat Sidi Mokhtar','Lakhssas','Moulay Abdallah','Oulad Jerrar',
  'Sebt Guerdane','Sidi Ahmed Ou Moussa','Sidi Bibi','Taliouine',
  'Taroudant','Tata','Taznakht','Tighmi','Tilit','Tioguete',
  'Tirhanimine','Tiznit','Tlata Sidi Bouguedra','Touizgui',
  'Toundoute','Ait Iaazza','Ait Milk','Akellay','Akermous',
  'Ameskroud','Boulimane','Bounrar','Douirane','Freija','Igli',
  'Igrir','Ihchech','Kasbah Sidi Mokhtar','Kelaa','Lakhsas',
  'Massa','Mezguida','Ntifte','Oumnas','Oumlil','Sfissif',
  'Sidi Bouzid','Sidi Fadl','Sidi Ouassay','Sidi Rbat','Tagadirt',
  'Tagmoute','Tahala','Tamdakhte','Tamrhakt','Tamri','Tanalt',
  'Tanghir','Tazenzalt','Tiznit','Tlata Sidi Bouguedra',
  'Afella Ighir','Aït Abdellah','Aït Ahmad','Aït Amira','Aït Igas',
  'Aït Makhlouf','Aït Ouazik','Aït Said','Anzi','Aoulouz','Arbaa Sahel',
  'Argan','Arghen','Asgaour','Askaoun','Asmama','Assaid','Assads',
  'Ighrem','Ikazane','Imsorane','Inchaden','Issen','Ouneine',
  'Sidi Ahmed Ou Ali','Sidi Brahim Ou Moussa','Sidi Moussa Aglou',
  'Tafedna','Tafingoult','Tafraout','Takad','Tamaloukt','Tamanar',
  'Tamanaute','Tamdaght','Tamga','Tamsoukt','Tanalt','Taroudant',
  'Tata','Tidsi','Tigerfemine','Tindouf','Tioughza','Tirsal',
  // ─── Région Drâa-Tafilalet ───
  'Errachidia','Zagora','Ouarzazate','Tinghir','Erfoud','Rissani',
  'Midelt','Rich','Aoufous','Goulmima','Tineghir','Kelaat M\'Gouna',
  'Boumalne Dadès','Merzouga','Alnif','Arfoud','Boudnib','El Jorf',
  'Fezna','Hassi Labied','Imider','Irara','Jorf','Madkour','Mellah',
  'Mezguida','Nkob','Sidi Ali','Skoura','Tagounit','Talsint',
  'Tandrara','Taous','Tazenakht','Tazzarine','Tinejdad','Todra',
  'Zaouia Sidi Driss','Aït Benhaddou','Aït Khaled','Boumalne',
  'Dades','El Fida','Er-Rachidia','Goulmima','Kelaa','Ksar Asfalou',
  'M\'Semrir','Ouarzazate','Rissani','Rich','Tazarin','Tinejdad',
  'Tinghir','Tizi N\'Tichka','Zagora','Zaouit Sidi Hamza',
  'Ait Herbil','Alnif','Amouguer','Aït Sedrat','Bou Anane',
  'Bou Izakarne','Dar Lhamra','El Khorbat','Fezzou','Ghassate',
  'Hannabou','Imilchil','Imlichil','Jebel Saghro','Kelaa des Megouna',
  'Ksar Ighnda','M\'Hamid El Ghizlane','Merzouga','Msemrir',
  'Nkob','Ouled Chaker','Oulad Driss','Outat Oulad El Haj',
  'Sidi Flah','Skoura','Tafraout N\'Ait Sidi Othmane','Taghbalt',
  'Tazroute','Tinghir','Tissint','Toundoute',
  // ─── Région Guelmim-Oued Noun ───
  'Guelmim','Tan-Tan','Assa','Zag','Tata','Akka','Fam El Hisn',
  'Aït Ouafella','Aït Belfaa','Bouizakarne','Fask','Lakhssas',
  'Lemsid','Sbaa','Sidi Ahmad El Aarouss','Tlata Taghramt',
  'Aït Boufoulen','Amtoudi','Anezi','Aoreora','Asrir','Azwafit',
  'Fask','Foum Zguid','Izerbi','Kentira','Lakhssas','Laâyoune',
  'Mtioua','Sidi Brahim','Tagante','Taghight','Tiznit','Tlata Taghramt',
  'Asaka','Assaka','Icht','Imremad','Laàyoune','Laâyoune Plage',
  'Tafnidilt','Taghjijt','Tagragra','Tahala','Tigehmert',
  // ─── Région Laâyoune-Sakia El Hamra ───
  'Laâyoune','Boujdour','Tarfaya','Smara','Es-Semara','Daoura',
  'El Marsa','Foum El Oued','Lemsid','Ras Jdir','Sabkhat Tah',
  'Haouza','Jraifia','Lamhiriz','Laâyoune','Laâyoune Plage',
  'Oum Dreyga','Sebt El Guerdane','Tah',
  // ─── Région Dakhla-Oued Ed-Dahab ───
  'Dakhla','Aousserd','Bir Gandouz','Bir Anzarane','El Argoub',
  'Gleibat El Foula','Imlili','Mijik','Bir Lahmar','Awserd',
  'Draa','Gharb','Tichla',
];

const CITIES = [...new Set(MOROCCO_CITIES)].sort((a, b) =>
  a.localeCompare(b, 'fr', { sensitivity: 'base' })
);

function normalize(s: string): string {
  return s.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '');
}

@Component({
  selector: 'app-city-input',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="city-input-wrap" style="position:relative">
      <div class="city-input-inner">
        <svg class="city-search-icon" width="14" height="14" viewBox="0 0 24 24" fill="none"
             stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
        </svg>
        <input
          type="text"
          class="form-input city-text-input"
          [value]="displayValue"
          (input)="onInput($any($event.target).value)"
          (focus)="onFocus()"
          (blur)="onBlur()"
          (keydown)="onKeyDown($event)"
          autocomplete="off"
        />
        @if (displayValue) {
          <button type="button" class="city-clear-btn" (mousedown)="clear($event)">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                 stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
            </svg>
          </button>
        }
      </div>

      @if (open && filtered.length > 0) {
        <ul class="city-dropdown" role="listbox">
          @for (city of filtered; track city; let i = $index) {
            <li class="city-option"
                [class.city-option-active]="i === activeIndex"
                (mousedown)="select(city, $event)"
                role="option">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                   stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
                   style="flex-shrink:0;color:var(--text-muted)">
                <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/>
                <circle cx="12" cy="10" r="3"/>
              </svg>
              <span [innerHTML]="highlight(city)"></span>
            </li>
          }
        </ul>
      }

      @if (open && query.length >= 1 && filtered.length === 0) {
        <div class="city-dropdown city-no-result">Aucune ville trouvée</div>
      }
    </div>
  `,
  styles: [`
    .city-input-inner { position:relative; display:flex; align-items:center; }
    .city-search-icon {
      position:absolute; left:12px; color:var(--text-muted);
      pointer-events:none; z-index:1;
    }
    .city-text-input { padding-left:34px !important; padding-right:32px !important; width:100%; }
    .city-clear-btn {
      position:absolute; right:10px; background:none; border:none;
      cursor:pointer; color:var(--text-muted); display:flex; align-items:center;
      padding:2px; border-radius:4px;
    }
    .city-clear-btn:hover { color:var(--text); }
    .city-dropdown {
      position:absolute; top:calc(100% + 4px); left:0; right:0;
      background:var(--surface); border:1px solid var(--border);
      border-radius:8px; box-shadow:0 8px 24px rgba(0,0,0,.15);
      z-index:1000; max-height:240px; overflow-y:auto;
      margin:0; padding:4px 0; list-style:none; scrollbar-width:thin;
    }
    .city-option {
      display:flex; align-items:center; gap:8px; padding:9px 14px;
      font-size:13.5px; cursor:pointer; color:var(--text); transition:background .1s;
    }
    .city-option:hover, .city-option-active { background:var(--surface2); }
    .city-no-result {
      padding:12px 16px; font-size:13px; color:var(--text-muted); text-align:center;
    }
    mark { background:transparent; color:var(--primary); font-weight:700; padding:0; }
  `],
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => CityInputComponent),
    multi: true
  }]
})
export class CityInputComponent implements ControlValueAccessor {
  readonly allCities = CITIES;
  displayValue = '';
  query = '';
  filtered: string[] = [];
  open = false;
  activeIndex = -1;

  private _onChange: (v: string) => void = () => {};
  onTouched: () => void = () => {};

  constructor(private el: ElementRef) {}

  @HostListener('document:mousedown', ['$event'])
  onDocClick(e: MouseEvent) {
    if (!this.el.nativeElement.contains(e.target)) this.open = false;
  }

  writeValue(value: string): void {
    this.displayValue = value || '';
    this.query = value || '';
  }

  registerOnChange(fn: (v: string) => void): void { this._onChange = fn; }
  registerOnTouched(fn: () => void): void { this.onTouched = fn; }

  onInput(val: string): void {
    this.displayValue = val;
    this.query = val;
    this.filtered = this.filter(val);
    this.open = true;
    this.activeIndex = -1;
    this._onChange(val);
  }

  onFocus(): void {
    this.filtered = this.query ? this.filter(this.query) : this.allCities.slice(0, 60);
    this.open = true;
  }

  onBlur(): void {
    this.onTouched();
    setTimeout(() => { this.open = false; }, 150);
  }

  onKeyDown(e: KeyboardEvent): void {
    if (!this.open) return;
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      this.activeIndex = Math.min(this.activeIndex + 1, this.filtered.length - 1);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      this.activeIndex = Math.max(this.activeIndex - 1, -1);
    } else if (e.key === 'Enter' && this.activeIndex >= 0) {
      e.preventDefault();
      this.select(this.filtered[this.activeIndex]);
    } else if (e.key === 'Escape') {
      this.open = false;
    }
  }

  select(city: string, e?: MouseEvent): void {
    e?.preventDefault();
    this.displayValue = city;
    this.query = city;
    this.open = false;
    this.activeIndex = -1;
    this._onChange(city);
  }

  clear(e: MouseEvent): void {
    e.preventDefault();
    this.displayValue = '';
    this.query = '';
    this.filtered = this.allCities.slice(0, 60);
    this.open = true;
    this._onChange('');
  }

  private filter(q: string): string[] {
    if (!q) return this.allCities.slice(0, 60);
    const nq = normalize(q);
    // Starts-with results first, then contains
    const starts = this.allCities.filter(c => normalize(c).startsWith(nq));
    const contains = this.allCities.filter(c => !normalize(c).startsWith(nq) && normalize(c).includes(nq));
    return [...starts, ...contains].slice(0, 80);
  }

  highlight(city: string): string {
    if (!this.query) return city;
    const nq = normalize(this.query);
    const nc = normalize(city);
    const idx = nc.indexOf(nq);
    if (idx < 0) return city;
    return (
      city.slice(0, idx) +
      '<mark>' + city.slice(idx, idx + this.query.length) + '</mark>' +
      city.slice(idx + this.query.length)
    );
  }
}
