﻿import { initializeComponent } from "../functions/init";
import showRating from "../components/show-rating";
import store from '../store';

import { mapState } from 'vuex';

initializeComponent("review-list", initReviewList);

function initReviewList(rootElement) {
    new Vue({
        el: '#' + rootElement.id,
        store,
        data: {
            Reviews: null
        },
        computed: {
            count: function () {
                return this.Reviews.length;
            },
            displayRating: function () {
                var reviewSum = 0;
                var count = 0;

                if (!this.Reviews.length) {
                    return;
                }

                for (var review of this.Reviews) {
                    if (this.getRating(review.Rating)) {
                        reviewSum += this.getRating(review.Rating);
                        count++;
                    }
                }

                return (reviewSum / count).toFixed(2);
            },
            averageRating: function () {
                var reviewSum = 0;
                var count = 0;

                if (!this.Reviews.length) {
                    return;
                }

                for (var review of this.Reviews) {
                    if (this.getRating(review.Rating)) {
                        reviewSum += this.getRating(review.Rating);
                        count++;
                    }
                }

                return ((reviewSum / count) * 20);
            },
            ...mapState([
                'updateIteration'
            ])
        },
        components: {
            showRating
        },
        methods: {
            fetchData: function () {
                this.Reviews = null;

                this.$http.get(location.href + '/reviews', {}).then((response) => {
                    if (response.data &&
                        response.data.Status &&
                        response.data.Status == 'success' &&
                        response.data.Data &&
                        response.data.Data.data &&
                        response.data.Data.data.Reviews) {

                        this.Reviews = response.data.Data.data.Reviews;
                    }
                    else {
                        this.Reviews = null;
                    }
                });
            },
            formatDate: function (dateField) {
                if (!dateField) {
                    return;
                }

                var dateLabel = '';
                var match = dateField.match(/Date\((.*)\)/);

                if (match && match.length) {
                    dateLabel = moment(match[1], 'x').format('MMM D, YYYY');
                }

                return dateLabel;
            },
            getRating: function (value) {
                if (!value) {
                    return null;
                }

                return Math.round(Math.abs(value) / 20);
            }
        },
        watch: {
            updateIteration: function () {
                this.fetchData();
            }
        },
        created: function () {
            this.fetchData();
        }
    });
}

