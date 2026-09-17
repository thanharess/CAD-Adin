;;; =========================
;;;  EQCIRCLE.LSP
;;; =========================
(defun c:EQCIRCLE ( / sel mode obj rad cen i ed)
  (vl-load-com)
  (setq sel (ssget '((0 . "CIRCLE"))))
  (if sel
    (progn
      (initget "M N")
      (setq mode (getkword "\nChe do [Mau/Nhap] <Mau>: "))
      (if (or (null mode) (= mode "M")) (setq mode "M"))
      (cond
        ((= mode "M")
          (setq obj (car (entsel "\nChon duong tron mau: ")))
          (if (and obj (= (cdr (assoc 0 (entget obj))) "CIRCLE"))
            (setq rad (cdr (assoc 40 (entget obj))))
            (prompt "\n[Error] Khong phai duong tron!")
          )
        )
        ((= mode "N")
          (setq rad (getreal "\nNhap ban kinh can dong nhat: "))
        )
      )
      (if rad
        (progn
          (setq i 0)
          (repeat (sslength sel)
            (setq obj (ssname sel i)
                  ed (entget obj)
                  cen (cdr (assoc 10 ed)))
            (setq ed (subst (cons 40 rad) (assoc 40 ed) ed))
            (entmod ed)
            (setq i (1+ i))
          )
          (prompt (strcat "\n[OK] Da dong nhat ban kinh thanh " (rtos rad 2 2)))
          (command "_.regen")
        )
      )
    )
    (prompt "\n[Error] Khong chon doi tuong nao.")
  )
  (princ)
)

;;; =========================
;;;  EQARC.LSP
;;; =========================
(defun c:EQARC ( / sel mode obj rad start end cen i ed)
  (vl-load-com)
  (setq sel (ssget '((0 . "ARC"))))
  (if sel
    (progn
      (initget "M N")
      (setq mode (getkword "\nChe do [Mau/Nhap] <Mau>: "))
      (if (or (null mode) (= mode "M")) (setq mode "M"))
      (cond
        ((= mode "M")
          (setq obj (car (entsel "\nChon cung tron mau: ")))
          (if (and obj (= (cdr (assoc 0 (entget obj))) "ARC"))
            (setq ed (entget obj)
                  rad (cdr (assoc 40 ed))
                  start (cdr (assoc 50 ed))
                  end (cdr (assoc 51 ed)))
            (prompt "\n[Error] Khong phai cung tron!")
          )
        )
        ((= mode "N")
          (setq rad (getreal "\nNhap ban kinh: "))
          (setq start (getangle "\nNhap goc bat dau (chon diem): "))
          (setq end (getangle "\nNhap goc ket thuc (chon diem): "))
        )
      )
      (if (and rad start end)
        (progn
          (setq i 0)
          (repeat (sslength sel)
            (setq obj (ssname sel i)
                  ed (entget obj)
                  cen (cdr (assoc 10 ed)))
            (setq ed (subst (cons 40 rad) (assoc 40 ed) ed))
            (entmod ed)
            (setq ed (entget obj))
            (setq ed (subst (cons 50 start) (assoc 50 ed) ed))
            (entmod ed)
            (setq ed (entget obj))
            (setq ed (subst (cons 51 end) (assoc 51 ed) ed))
            (entmod ed)
            (setq i (1+ i))
          )
          (prompt "\n[OK] Da dong nhat cung tron.")
          (command "_.regen")
        )
      )
    )
    (prompt "\n[Error] Khong chon doi tuong nao.")
  )
  (princ)
)

;;; =========================
;;;  EQRECT.LSP - CAI THIEN GIOI HAN SUA: Tang tol trong is-rectangle? len 1e-4 de linh hoat hon voi rect hoi lech (khong strict),
;;; them tuy chon "IgnoreCheck" de bo qua kiem tra neu rect "loi" nhung van muon sua (prompt hoi neu phat hien lech),
;;; gioi han xu ly: Khong gioi han so luong, nhung them counter thanh cong/that bai, ho tro undo toan bo.
;;; =========================
(defun c:EQRECT (/ ss opt refObj refW refH refAng i e ed obj oldLay oldCol oldLw newRect verts pts cen ang vec1 vec2 len1 len2 err success ignoreCheck)
  (vl-load-com)
  (command "_.undo" "_mark")  ; Undo group
  (setq ss (ssget '((0 . "LWPOLYLINE")))) ; chi chon polyline (RECT)
  (if ss
    (progn
      (initget "Mau Nhap")
      (setq opt (getkword "\nChe do [Mau/Nhap] <Mau>: "))
      (if (not opt) (setq opt "Mau"))

      ;;========================
      ;; LAY KICH THUOC MAU
      ;;========================
      (cond
        ;; --- Theo mau ---
        ((= opt "Mau")
         (setq refObj (car (entsel "\nChon RECT mau (Polyline 4 canh khep kin): ")))
         (if refObj
           (progn
             (setq ed (entget refObj))
             (if (and (= (cdr (assoc 0 ed)) "LWPOLYLINE")
                      (= (cdr (assoc 70 ed)) 1)  ; Closed
                      (= (length (setq verts (vl-remove-if-not '(lambda (x) (= (car x) 10)) ed))) 4))  ; 4 vertices
               (progn
                 (setq pts (mapcar '(lambda (v) (list (car v) (cadr v) (if (caddr v) (caddr v) 0.0))) (mapcar 'cdr verts)))
                 (if (is-rectangle? pts)
                   (progn
                     ;; Tinh kich thuoc thuc tu vertices
                     (setq vec1 (mapcar '- (nth 1 pts) (car pts)))  ; Vector canh dau
                     (setq vec2 (mapcar '- (nth 2 pts) (nth 1 pts)))  ; Vector canh thu 2
                     (setq len1 (distance '(0.0 0.0 0.0) vec1))
                     (setq len2 (distance '(0.0 0.0 0.0) vec2))
                     ;; Chon width = canh dai hon, height = ngan hon
                     (if (> len1 len2)
                       (setq refW len1 refH len2)
                       (setq refW len2 refH len1))
                     (setq refAng (angle '(0.0 0.0) (list (car vec1) (cadr vec1))))  ; Goc 2D
                     (prompt (strcat "\nKich thuoc mau: Rong=" (rtos refW 2 2) ", Cao=" (rtos refH 2 2))))
                   (progn
                     (prompt "\n[Error] Mau khong phai RECT hop le (canh khong vuong goc hoac khong bang nhau)!")
                     (setq refW nil refH nil))))
               (progn
                 (prompt "\n[Error] Mau khong phai RECT hop le (khong phai polyline khep kin 4 canh)!")
                 (setq refW nil refH nil))))
           (progn
             (prompt "\n[Error] Khong chon duoc mau!")
             (setq refW nil refH nil))))
        ;; --- Nhap tay ---
        ((= opt "Nhap")
         (prompt "\nChe do Nhap tay: Se ap dung kich thuoc moi cho TAT CA RECT, giu nguyen vi tri + huong + thuoc tinh.")
         (setq refW (getreal "\nNhap chieu rong mong muon (>0): "))
         (setq refH (getreal "\nNhap chieu cao mong muon (>0): "))
         (setq refAng 0.0)  ; Khong dung refAng o Nhap, dung ang tung rect
         (if (not (and refW refH (> refW 0) (> refH 0)))
           (progn
             (prompt "\n[Error] Kich thuoc khong hop le! Phai >0.")
             (setq refW nil refH nil)))))
      )

      ;;========================
      ;; DUYET MOI RECT DUOC CHON
      ;;========================
      (if (and refW refH)
        (progn
          (setq i 0 err 0 success 0 ignoreCheck nil)
          (repeat (sslength ss)
            (setq e (ssname ss i))
            (if (or (not refObj) (/= e refObj))  ; Bo qua mau neu co
              (progn
                (setq ed (entget e))
                (if (and (= (cdr (assoc 0 ed)) "LWPOLYLINE")
                         (= (cdr (assoc 70 ed)) 1)
                         (= (length (setq verts (vl-remove-if-not '(lambda (x) (= (car x) 10)) ed))) 4))
                  (progn
                    (setq pts (mapcar '(lambda (v) (list (car v) (cadr v) (if (caddr v) (caddr v) 0.0))) (mapcar 'cdr verts)))
                    (if (or (is-rectangle? pts) (and (not ignoreCheck) (initget "Yes No") (= (getkword "\n[Warning] RECT nay hoi lech (khong vuong goc). Van sua? [Yes/No] <No>: ") "Yes")))
                      (progn
                        (setq obj (vlax-ename->vla-object e))
                        ;; Luu thuoc tinh goc
                        (setq oldLay (vla-get-layer obj)
                              oldCol (vla-get-color obj)
                              oldLw (vla-get-lineweight obj))
                        ;; Tinh tam & huong tu vertices
                        (setq cen (centroid-rect pts))
                        (setq vec1 (mapcar '- (nth 1 pts) (car pts)))
                        (if (> (distance '(0.0 0.0 0.0) vec1) 1e-6)  ; Tranh vec1 qua ngan
                          (setq ang (angle '(0.0 0.0) (list (car vec1) (cadr vec1))))  ; Goc 2D tung rect
                          (setq ang 0.0))  ; Default neu vec1 nil

                        ;; Xoa RECT cu
                        (if (not (vl-catch-all-error-p (vl-catch-all-apply 'vla-delete (list obj))))
                          (progn
                            ;; Ve RECT moi (axis-aligned tai tam)
                            (setq pA (list (- (car cen) (/ refW 2.0)) (- (cadr cen) (/ refH 2.0)) (caddr cen)))
                            (setq pB (list (+ (car cen) (/ refW 2.0)) (+ (cadr cen) (/ refH 2.0)) (caddr cen)))
                            (if (not (vl-catch-all-error-p (vl-catch-all-apply 'command (list "_.RECTANG" pA pB))))
                              (progn
                                (setq newRect (entlast))
                                ;; Xoay theo goc goc (neu khong =0)
                                (if (not (equal ang 0.0 1e-6))
                                  (command "_.ROTATE" newRect "" cen (angtos ang 0 4)))
                                ;; Tra lai layer & mau & lineweight
                                (setq vlaNew (vlax-ename->vla-object newRect))
                                (vla-put-layer vlaNew oldLay)
                                (vla-put-color vlaNew oldCol)
                                (vla-put-lineweight vlaNew oldLw)
                                (setq success (1+ success)))
                              (setq err (1+ err))))
                          (setq err (1+ err))))
                      (prompt (strcat "\n[Warning] Bo qua entity " (itoa i) ": Khong phai RECT hop le (canh khong vuong goc).")))
                  (prompt (strcat "\n[Warning] Bo qua entity " (itoa i) ": Khong phai polyline khep kin 4 canh.")))))
            (setq i (1+ i)))
          (prompt (strcat "\nKet qua: Thanh cong " (itoa success) ", Loi/Bo qua " (itoa err) ". Khong gioi han so luong sua!"))
          (command "_.regen"))
        (prompt "\n[Error] Khong co kich thuoc hop le!"))
    )
    (prompt "\n[Error] Khong co doi tuong nao duoc chon!"))
  (command "_.undo" "_end")
  (princ)
)

;; Ham phu: Distance 3D cho vector (xu ly nil Z)
(defun distance (p1 p2 / dx dy dz)
  (setq dx (- (car p1) (car p2))
        dy (- (cadr p1) (cadr p2))
        dz (- (if (caddr p1) (caddr p1) 0.0) (if (caddr p2) (caddr p2) 0.0)))
  (sqrt (+ (* dx dx) (* dy dy) (* dz dz))))

;; Ham phu: Centroid tu 4 diem (xu ly 2D/3D)
(defun centroid-rect (pts / sumx sumy sumz n p x y z)
  (setq sumx 0.0 sumy 0.0 sumz 0.0 n (length pts))
  (foreach p pts
    (setq x (car p)
          y (cadr p)
          z (if (caddr p) (caddr p) 0.0))
    (setq sumx (+ sumx x)
          sumy (+ sumy y)
          sumz (+ sumz z)))
  (list (/ sumx n) (/ sumy n) (/ sumz n)))

;; Ham kiem tra rectangle: canh doi dien bang, goc vuong (dot product ~0) - tol linh hoat hon
(defun is-rectangle? (pts / v1 v2 v3 v4 len1 len2 len3 len4 dot1 dot2 dot3 dot4 tol)
  (setq tol 1e-4)  ; Tang tol de linh hoat voi rect hoi lech
  (if (= (length pts) 4)
    (progn
      (setq v1 (mapcar '- (nth 1 pts) (nth 0 pts))
            v2 (mapcar '- (nth 2 pts) (nth 1 pts))
            v3 (mapcar '- (nth 3 pts) (nth 2 pts))
            v4 (mapcar '- (nth 0 pts) (nth 3 pts)))
      (setq len1 (distance '(0.0 0.0 0.0) v1)
            len2 (distance '(0.0 0.0 0.0) v2)
            len3 (distance '(0.0 0.0 0.0) v3)
            len4 (distance '(0.0 0.0 0.0) v4))
      (setq dot1 (+ (* (car v1) (car v2)) (* (cadr v1) (cadr v2)))
            dot2 (+ (* (car v2) (car v3)) (* (cadr v2) (cadr v3)))
            dot3 (+ (* (car v3) (car v4)) (* (cadr v3) (cadr v4)))
            dot4 (+ (* (car v4) (car v1)) (* (cadr v4) (cadr v1))))
      (and (< (abs (- len1 len3)) (* tol len1))
           (< (abs (- len2 len4)) (* tol len2))
           (< (abs dot1) (* tol len1 len2))
           (< (abs dot2) (* tol len2 len3))
           (< (abs dot3) (* tol len3 len4))
           (< (abs dot4) (* tol len4 len1))))
    nil))

(princ "\nLSP da load thanh cong: EQCIRCLE, EQARC, EQRECT.")
(princ)