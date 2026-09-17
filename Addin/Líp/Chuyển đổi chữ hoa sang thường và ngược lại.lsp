;;; ============================================================
;;; UCLV.lsp
;;; Chuyển HOA / thường TEXT & MTEXT – hỗ trợ TIẾNG VIỆT ĐẦY ĐỦ
;;; Không lỗi \P, \H, \C, \f (giữ nguyên format MTEXT)
;;; Lệnh: UCLV
;;; ============================================================

(vl-load-com)

;; ---------- COPY MTEXT CODE ----------
(defun copy-mtext-code (str idx / j)
  (setq j idx)
  (if (= (substr str idx 2) "\\P")
    "\\P"
    (progn
      (while (and (<= j (strlen str))
                  (/= (substr str j 1) ";"))
        (setq j (1+ j))
      )
      (substr str idx (- (1+ j) idx))
    )
  )
)

;; ---------- HOA 1 KÝ TỰ ----------
(defun upcase-char-vn (ch)
  (cond
    ((member ch '("a")) "A") ((member ch '("à")) "À") ((member ch '("á")) "Á")
    ((member ch '("ả")) "Ả") ((member ch '("ã")) "Ã") ((member ch '("ạ")) "Ạ")
    ((member ch '("ă")) "Ă") ((member ch '("ằ")) "Ằ") ((member ch '("ắ")) "Ắ")
    ((member ch '("ẳ")) "Ẳ") ((member ch '("ẵ")) "Ẵ") ((member ch '("ặ")) "Ặ")
    ((member ch '("â")) "Â") ((member ch '("ầ")) "Ầ") ((member ch '("ấ")) "Ấ")
    ((member ch '("ẩ")) "Ẩ") ((member ch '("ẫ")) "Ẫ") ((member ch '("ậ")) "Ậ")
    ((member ch '("d")) "D") ((member ch '("đ")) "Đ")
    ((member ch '("e")) "E") ((member ch '("è")) "È") ((member ch '("é")) "É")
    ((member ch '("ẻ")) "Ẻ") ((member ch '("ẽ")) "Ẽ") ((member ch '("ẹ")) "Ẹ")
    ((member ch '("ê")) "Ê") ((member ch '("ề")) "Ề") ((member ch '("ế")) "Ế")
    ((member ch '("ể")) "Ể") ((member ch '("ễ")) "Ễ") ((member ch '("ệ")) "Ệ")
    ((member ch '("i")) "I") ((member ch '("ì")) "Ì") ((member ch '("í")) "Í")
    ((member ch '("ỉ")) "Ỉ") ((member ch '("ĩ")) "Ĩ") ((member ch '("ị")) "Ị")
    ((member ch '("o")) "O") ((member ch '("ò")) "Ò") ((member ch '("ó")) "Ó")
    ((member ch '("ỏ")) "Ỏ") ((member ch '("õ")) "Õ") ((member ch '("ọ")) "Ọ")
    ((member ch '("ô")) "Ô") ((member ch '("ồ")) "Ồ") ((member ch '("ố")) "Ố")
    ((member ch '("ổ")) "Ổ") ((member ch '("ỗ")) "Ỗ") ((member ch '("ộ")) "Ộ")
    ((member ch '("ơ")) "Ơ") ((member ch '("ờ")) "Ờ") ((member ch '("ớ")) "Ớ")
    ((member ch '("ở")) "Ở") ((member ch '("ỡ")) "Ỡ") ((member ch '("ợ")) "Ợ")
    ((member ch '("u")) "U") ((member ch '("ù")) "Ù") ((member ch '("ú")) "Ú")
    ((member ch '("ủ")) "Ủ") ((member ch '("ũ")) "Ũ") ((member ch '("ụ")) "Ụ")
    ((member ch '("ư")) "Ư") ((member ch '("ừ")) "Ừ") ((member ch '("ứ")) "Ứ")
    ((member ch '("ử")) "Ử") ((member ch '("ữ")) "Ữ") ((member ch '("ự")) "Ự")
    ((member ch '("y")) "Y") ((member ch '("ỳ")) "Ỳ") ((member ch '("ý")) "Ý")
    ((member ch '("ỷ")) "Ỷ") ((member ch '("ỹ")) "Ỹ") ((member ch '("ỵ")) "Ỵ")
    ((and (>= ch "a") (<= ch "z")) (strcase ch))
    (t ch)
  )
)

;; ---------- THƯỜNG 1 KÝ TỰ ----------
(defun downcase-char-vn (ch)
  (cond
    ((member ch '("A")) "a") ((member ch '("À")) "à") ((member ch '("Á")) "á")
    ((member ch '("Ả")) "ả") ((member ch '("Ã")) "ã") ((member ch '("Ạ")) "ạ")
    ((member ch '("Ă")) "ă") ((member ch '("Ằ")) "ằ") ((member ch '("Ắ")) "ắ")
    ((member ch '("Ẳ")) "ẳ") ((member ch '("Ẵ")) "ẵ") ((member ch '("Ặ")) "ặ")
    ((member ch '("Â")) "â") ((member ch '("Ầ")) "ầ") ((member ch '("Ấ")) "ấ")
    ((member ch '("Ẩ")) "ẩ") ((member ch '("Ẫ")) "ẫ") ((member ch '("Ậ")) "ậ")
    ((member ch '("D")) "d") ((member ch '("Đ")) "đ")
    ((member ch '("E")) "e") ((member ch '("È")) "è") ((member ch '("É")) "é")
    ((member ch '("Ẻ")) "ẻ") ((member ch '("Ẽ")) "ẽ") ((member ch '("Ẹ")) "ẹ")
    ((member ch '("Ê")) "ê") ((member ch '("Ề")) "ề") ((member ch '("Ế")) "ế")
    ((member ch '("Ể")) "ể") ((member ch '("Ễ")) "ễ") ((member ch '("Ệ")) "ệ")
    ((member ch '("I")) "i") ((member ch '("Ì")) "ì") ((member ch '("Í")) "í")
    ((member ch '("Ỉ")) "ỉ") ((member ch '("Ĩ")) "ĩ") ((member ch '("Ị")) "ị")
    ((member ch '("O")) "o") ((member ch '("Ò")) "ò") ((member ch '("Ó")) "ó")
    ((member ch '("Ỏ")) "ỏ") ((member ch '("Õ")) "õ") ((member ch '("Ọ")) "ọ")
    ((member ch '("Ô")) "ô") ((member ch '("Ồ")) "ồ") ((member ch '("Ố")) "ố")
    ((member ch '("Ổ")) "ổ") ((member ch '("Ỗ")) "ỗ") ((member ch '("Ộ")) "ộ")
    ((member ch '("Ơ")) "ơ") ((member ch '("Ờ")) "ờ") ((member ch '("Ớ")) "ớ")
    ((member ch '("Ở")) "ở") ((member ch '("Ỡ")) "ỡ") ((member ch '("Ợ")) "ợ")
    ((member ch '("U")) "u") ((member ch '("Ù")) "ù") ((member ch '("Ú")) "ú")
    ((member ch '("Ủ")) "ủ") ((member ch '("Ũ")) "ũ") ((member ch '("Ụ")) "ụ")
    ((member ch '("Ư")) "ư") ((member ch '("Ừ")) "ừ") ((member ch '("Ứ")) "ứ")
    ((member ch '("Ử")) "ử") ((member ch '("Ữ")) "ữ") ((member ch '("Ự")) "ự")
    ((member ch '("Y")) "y") ((member ch '("Ỳ")) "ỳ") ((member ch '("Ý")) "ý")
    ((member ch '("Ỷ")) "ỷ") ((member ch '("Ỹ")) "ỹ") ((member ch '("Ỵ")) "ỵ")
    ((and (>= ch "A") (<= ch "Z")) (strcase ch T))
    (t ch)
  )
)

;; ---------- HOA CHUỖI ----------
(defun str-upcase-vn-keep (str / result i ch code)
  (setq result "" i 1)
  (while (<= i (strlen str))
    (setq ch (substr str i 1))
    (if (= ch "\\")
      (progn
        (setq code (copy-mtext-code str i))
        (setq result (strcat result code))
        (setq i (+ i (strlen code)))
      )
      (progn
        (setq result (strcat result (upcase-char-vn ch)))
        (setq i (1+ i))
      )
    )
  )
  result
)

;; ---------- THƯỜNG CHUỖI ----------
(defun str-downcase-vn-keep (str / result i ch code)
  (setq result "" i 1)
  (while (<= i (strlen str))
    (setq ch (substr str i 1))
    (if (= ch "\\")
      (progn
        (setq code (copy-mtext-code str i))
        (setq result (strcat result code))
        (setq i (+ i (strlen code)))
      )
      (progn
        (setq result (strcat result (downcase-char-vn ch)))
        (setq i (1+ i))
      )
    )
  )
  result
)

;; ---------- LỆNH CHÍNH ---------
(defun c:Textchangeuppertext (/ ss opt i obj txt newtxt cnt)
  ;; Hiển thị menu chọn kiểu
  (princ "\nChọn kiểu chuyển đổi:")
  (princ "\n 1. Hoa (chữ in HOA)")
  (princ "\n 2. Thuong (chữ thường)")
  (initget "Hoa Thuong")
  (setq opt (getkword "\nNhập lựa chọn [Hoa/Thuong] <Hoa>: "))
  (if (null opt) (setq opt "Hoa"))

  ;; Chọn đối tượng
  (princ "\nChọn các đối tượng TEXT hoặc MTEXT cần chuyển đổi: ")
  (setq ss (ssget '((0 . "TEXT,MTEXT"))))
  (if (null ss)
    (progn
      (princ "\nKhông có đối tượng nào được chọn.")
      (exit)
    )
  )

  ;; Xử lý từng đối tượng
  (setq i 0 cnt 0)
  (repeat (sslength ss)
    (setq obj (vlax-ename->vla-object (ssname ss i)))
    (setq txt (vla-get-TextString obj))

    (setq newtxt
      (if (= opt "Hoa")
        (str-upcase-vn-keep txt)
        (str-downcase-vn-keep txt)
      )
    )

    (if (/= txt newtxt)
      (progn
        (vla-put-TextString obj newtxt)
        (setq cnt (1+ cnt))
      )
    )

    (setq i (1+ i))
  )

  ;; Thông báo kết quả
  (princ (strcat "\nHoàn tất – đã xử lý " (itoa cnt) " đối tượng."))
  (princ)
)
