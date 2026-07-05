import { Component, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CompareService } from '../../../core/services/compare';

@Component({
  selector: 'app-compare-floater',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './compare-floater.html',
  styleUrls: ['../home.scss']
})
export class CompareFloaterComponent {
  public readonly compareService = inject(CompareService);
}