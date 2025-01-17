// reservations.component.ts
// Author: František Nečas

import { Component, OnInit } from '@angular/core';
import { BoardGamesService } from "../../shared/services/board-games.service";
import { ToastrService } from "ngx-toastr";
import { Reservation, ReservationState } from "../../models/board-games/reservation.model";
import { FormControl } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { BoardGamesStoreService } from "../../shared/services/board-games-store.service";

@Component({
  selector: 'app-reservations',
  templateUrl: './reservations.component.html',
  styleUrls: ['./reservations.component.css']
})
export class ReservationsComponent implements OnInit {
  reservations: Reservation[]
  filterKeys: [string, ReservationState][] = [
    ["Pouze právě běžící", ReservationState.Current],
    ["Pouze po skončení platnosti", ReservationState.Expired],
    ["Pouze dokončené", ReservationState.Done]
  ]
  reservationFilterForm = new FormControl("---");
  reservationFilter: ReservationState | undefined = undefined;

  constructor(private boardGamesService: BoardGamesService, private toastrService: ToastrService,
              private router: Router, private activatedRoute: ActivatedRoute,
              private storeService: BoardGamesStoreService) {
  }

  ngOnInit(): void {
    this.reservationFilter = this.storeService.getReservationFilter();
    let formValue = this.filterKeys.find(k => k[1] == this.reservationFilter);
    this.reservationFilterForm.reset(formValue ? formValue[0] : "---");

    this.reservationFilterForm.valueChanges.subscribe(value => {
      let filter = this.filterKeys.find(k => k[0] == value);
      this.reservationFilter = filter ? filter[1] : undefined;
      this.fetchReservations();
    })
    this.fetchReservations();
  }

  fetchReservations(): void {
    this.boardGamesService.getReservations(this.reservationFilter).subscribe(
      reservations => {
        // FIXME: Possibly move this sort to backend (also related to pagination)
        this.reservations = reservations.sort((a, b) => {
          if (a.madeOn > b.madeOn) {
            return -1;
          } else if (a.madeOn < b.madeOn) {
            return 1;
          } else {
            return 0;
          }
        });
      },
      err => {
        console.log(err);
        this.toastrService.error("Načtení rezervací se nezdařilo. Jsi přihlášen*a?");
      }
    )
  }

  ngOnDestroy(): void {
    this.storeService.saveReservationFilter(this.reservationFilter);
  }

  navigateToReservation(reservation: Reservation): void {
    this.router.navigate([`./${reservation.id}`], {relativeTo: this.activatedRoute}).then();
  }
}
